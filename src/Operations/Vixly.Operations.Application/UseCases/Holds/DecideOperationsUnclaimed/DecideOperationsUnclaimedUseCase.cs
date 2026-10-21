using Vixly.Operations.Application.Audit;
using Vixly.Operations.Application.Holds;
using Vixly.Operations.Application.Inspection;
using Vixly.Operations.Application.Messaging;
using Vixly.Operations.Application.Ports;
using Vixly.Operations.Application.Receiving;
using Vixly.Operations.Application.Security;
using Vixly.Operations.Application.Tracking;
using Vixly.Operations.Domain.Access;
using Vixly.Operations.Domain.Common;
using Vixly.Operations.Domain.Holds;
using Vixly.Operations.Domain.Inspection;
using Vixly.Operations.Domain.Locations;
using Vixly.Operations.Domain.Receiving;
using Vixly.Operations.Domain.Settings;
using Vixly.Operations.Domain.Tracking;
using Vixly.Shared.Kernel.Domain;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Holds.DecideOperationsUnclaimed;

// =========================================================
// FILE: DecideOperationsUnclaimedUseCase.cs
//
// PURPOSE:
// The Owner or an administrator decides a package unclaimed past the company's days: return to sender or dispose of (Slice 3E, Q-R6).
//
// RESPONSIBILITIES:
// - Run the rule and return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - Needs ops.holds.manage for the parcel's warehouse and the Owner or an administrator
// - The customer had reminders at day 7, 14 and 21 and a final notice at day 25
// =========================================================
public sealed class DecideOperationsUnclaimedUseCase
{
    private readonly OperationsOwnerCheck _owner;

    private readonly OperationsReceivingScope _scope;

    private readonly IOperationsTrackingRepository _tracking;

    private readonly IOperationsSettingsRepository _settings;

    private readonly IOperationsHoldRepository _holds;

    private readonly OperationsInspectionSteps _steps;

    private readonly OperationsAuditTrail _audit;

    private readonly IOperationsUnitOfWork _unitOfWork;

    private readonly TimeProvider _timeProvider;

    public DecideOperationsUnclaimedUseCase(
        OperationsOwnerCheck owner,
        OperationsReceivingScope scope,
        IOperationsTrackingRepository tracking,
        IOperationsSettingsRepository settings,
        IOperationsHoldRepository holds,
        OperationsInspectionSteps steps,
        OperationsAuditTrail audit,
        IOperationsUnitOfWork unitOfWork,
        TimeProvider timeProvider)
    {
        _owner = owner ?? throw new ArgumentNullException(nameof(owner));
        _scope = scope ?? throw new ArgumentNullException(nameof(scope));
        _tracking = tracking ?? throw new ArgumentNullException(nameof(tracking));
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        _holds = holds ?? throw new ArgumentNullException(nameof(holds));
        _steps = steps ?? throw new ArgumentNullException(nameof(steps));
        _audit = audit ?? throw new ArgumentNullException(nameof(audit));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public async Task<DecideOperationsUnclaimedResponse> ExecuteAsync(
        OperationsAuthorizationContext context,
        DecideOperationsUnclaimedRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        if (await _owner.LoadAsync(context, cancellationToken) is not { } access)
        {
            return DecideOperationsUnclaimedResponse.Failure(OperationsAccessErrors.SignInRequired);
        }

        if (await _tracking.GetParcelAsync(request.ParcelId, cancellationToken) is not { WarehouseId: { } warehouseId } parcel
            || await _scope.CoveredAsync(access, OperationsPermissions.HoldsManage, warehouseId, cancellationToken) is not { } warehouse)
        {
            return DecideOperationsUnclaimedResponse.Failure(OperationsReceivingErrors.ParcelNotFound);
        }

        if (!OperationsCommandKey.IsValid(request.CommandKey))
        {
            return DecideOperationsUnclaimedResponse.Failure(OperationsTrackingErrors.IdempotencyKeyInvalid);
        }

        string key = "unc:" + request.CommandKey;

        if (await _tracking.FindStoredByKeyAsync(key + ":final", cancellationToken) is { } earlier && earlier.SubjectId == parcel.Id)
        {
            return DecideOperationsUnclaimedResponse.Success();
        }

        if (!(access.IsOwner || access.IsAdministrator))
        {
            return DecideOperationsUnclaimedResponse.Failure(OperationsHoldErrors.ApproverNotAllowed);
        }

        // Slice 4D step 3: or sold at auction (Q-D4: the company runs it; the note may say who bought it and for how much).
        if (request.Decision is not ("return" or "dispose" or "auction"))
        {
            return DecideOperationsUnclaimedResponse.Failure(OperationsHoldErrors.DecisionInvalid);
        }

        DateTimeOffset utcNow = _timeProvider.GetUtcNow();
        int days = (await _settings.FindAsync(cancellationToken))?.UnclaimedDays ?? OperationsHoldLimits.DefaultUnclaimedDays;

        // Still at its warehouse, still waiting for its customer (or unidentified), for longer than the company's days.
        // An answer of the customer waiting for staff is reviewed first, never overtaken by the decision.
        if (!OperationsUnclaimed.IsUnclaimed(parcel, days, utcNow) || await _holds.HasOpenAnswerAsync(parcel.Id, cancellationToken))
        {
            return DecideOperationsUnclaimedResponse.Failure(OperationsHoldErrors.NotUnclaimed);
        }

        string? note;

        try
        {
            note = OperationsDeclarationText.Optional(request.Note, OperationsHoldLimits.NoteMaxLength);
        }
        catch (DomainException)
        {
            return DecideOperationsUnclaimedResponse.Failure(OperationsHoldErrors.TextInvalid);
        }

        // The last milestone, then every hold ends with it (ADR-0018).
        List<(string Key, string Code, string? Argument, string? Value, string? Note)> steps = new()
        {
            (key + ":final", request.Decision switch
            {
                "return" => OperationsTrackingCodes.ReturnedToSender,
                "auction" => OperationsTrackingCodes.Auctioned,
                _ => OperationsTrackingCodes.Disposed
            }, null, null, note)
        };

        foreach (OperationsHoldReasons reason in Enum.GetValues<OperationsHoldReasons>().Where(reason => reason != OperationsHoldReasons.None && parcel.HoldReasons.HasFlag(reason)))
        {
            steps.Add((key + ":release:" + reason, OperationsTrackingCodes.HoldReleased, OperationsTrackingCode.ReasonArgument, reason.ToString(), note));
        }

        if (await _steps.RecordAsync(parcel.Id, access, warehouse, utcNow, cancellationToken, steps.ToArray()) is { } refused)
        {
            return DecideOperationsUnclaimedResponse.Failure(refused);
        }

        _audit.Record(access.User.Id, "ops.holds.unclaimed_decided", nameof(OperationsParcel), parcel.Id, after: new { parcel = parcel.Number, decision = request.Decision, note, days });

        if (await _unitOfWork.SaveChangesAsync(cancellationToken))
        {
            return DecideOperationsUnclaimedResponse.Success();
        }

        _unitOfWork.DiscardChanges();
        return await _tracking.FindStoredByKeyAsync(key + ":final", cancellationToken) is { } raced && raced.SubjectId == parcel.Id
            ? DecideOperationsUnclaimedResponse.Success()
            : DecideOperationsUnclaimedResponse.Failure(OperationsCommonErrors.ConcurrencyConflict);
    }
}
