using Vixly.Operations.Application.Audit;
using Vixly.Operations.Application.Inspection;
using Vixly.Operations.Application.Ports;
using Vixly.Operations.Application.Receiving;
using Vixly.Operations.Application.Security;
using Vixly.Operations.Application.Tracking;
using Vixly.Operations.Domain.Access;
using Vixly.Operations.Domain.Common;
using Vixly.Operations.Domain.Inspection;
using Vixly.Operations.Domain.Locations;
using Vixly.Operations.Domain.Tracking;
using Vixly.Shared.Kernel.Domain;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Inspection.DecideOperationsDisposal;

// =========================================================
// FILE: DecideOperationsDisposalUseCase.cs
//
// PURPOSE:
// The Owner or an administrator approves or declines disposing of a refused parcel (Slice 3C, Q-C3).
//
// RESPONSIBILITIES:
// - Run the rule and return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - Only the Owner or an administrator, never the inspector who asked for the disposal (a second person)
// - Needs ops.holds.manage for the parcel's warehouse
// =========================================================
public sealed class DecideOperationsDisposalUseCase
{
    private readonly OperationsOwnerCheck _owner;

    private readonly OperationsReceivingScope _scope;

    private readonly IOperationsInspectionRepository _inspections;

    private readonly IOperationsTrackingRepository _tracking;

    private readonly OperationsInspectionSteps _steps;

    private readonly OperationsAuditTrail _audit;

    private readonly IOperationsUnitOfWork _unitOfWork;

    private readonly TimeProvider _timeProvider;

    public DecideOperationsDisposalUseCase(
        OperationsOwnerCheck owner,
        OperationsReceivingScope scope,
        IOperationsInspectionRepository inspections,
        IOperationsTrackingRepository tracking,
        OperationsInspectionSteps steps,
        OperationsAuditTrail audit,
        IOperationsUnitOfWork unitOfWork,
        TimeProvider timeProvider)
    {
        _owner = owner ?? throw new ArgumentNullException(nameof(owner));
        _scope = scope ?? throw new ArgumentNullException(nameof(scope));
        _inspections = inspections ?? throw new ArgumentNullException(nameof(inspections));
        _tracking = tracking ?? throw new ArgumentNullException(nameof(tracking));
        _steps = steps ?? throw new ArgumentNullException(nameof(steps));
        _audit = audit ?? throw new ArgumentNullException(nameof(audit));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public async Task<DecideOperationsDisposalResponse> ExecuteAsync(
        OperationsAuthorizationContext context,
        DecideOperationsDisposalRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        if (await _owner.LoadAsync(context, cancellationToken) is not { } access)
        {
            return DecideOperationsDisposalResponse.Failure(OperationsAccessErrors.SignInRequired);
        }

        if (await _inspections.GetInspectionAsync(request.InspectionId, cancellationToken) is not { } inspection
            || await _scope.CoveredAsync(access, OperationsPermissions.HoldsManage, inspection.WarehouseId, cancellationToken) is not { } warehouse)
        {
            return DecideOperationsDisposalResponse.Failure(OperationsInspectionErrors.NotFound);
        }

        if (!OperationsCommandKey.IsValid(request.CommandKey))
        {
            return DecideOperationsDisposalResponse.Failure(OperationsTrackingErrors.IdempotencyKeyInvalid);
        }

        string key = "dsp:" + request.CommandKey;

        if (await _tracking.FindStoredByKeyAsync(key + ":decided", cancellationToken) is { } earlier && earlier.SubjectId == inspection.ParcelId)
        {
            return DecideOperationsDisposalResponse.Success();
        }

        string? note = null;
        DateTimeOffset utcNow = _timeProvider.GetUtcNow();

        try
        {
            note = OperationsDeclarationText.Optional(request.Note, OperationsInspectionLimits.CallNotesMaxLength);

            if (request.Approve)
            {
                inspection.ApproveDisposal(access.User.Id, access.IsOwner || access.IsAdministrator, utcNow);
            }
            else
            {
                inspection.DeclineDisposal(access.User.Id, access.IsOwner || access.IsAdministrator, utcNow);
            }
        }
        catch (DomainException exception)
        {
            return DecideOperationsDisposalResponse.Failure(exception.Error);
        }

        // Approved: the inspection's outcome, then the parcel's last milestone; the refusal hold ends with it (ADR-0018).
        // Declined: the refusal hold ends and the inspector records another outcome.
        Error? refused = request.Approve
            ? await _steps.RecordAsync(inspection.ParcelId, access, warehouse, utcNow, cancellationToken,
                (key + ":completed", OperationsTrackingCodes.InspectionCompleted, "result", "refused_disposed", null),
                (key + ":disposed", OperationsTrackingCodes.Disposed, null, null, note),
                (key + ":decided", OperationsTrackingCodes.HoldReleased, OperationsTrackingCode.ReasonArgument, nameof(OperationsHoldReasons.Refused), note))
            : await _steps.RecordAsync(inspection.ParcelId, access, warehouse, utcNow, cancellationToken,
                (key + ":decided", OperationsTrackingCodes.HoldReleased, OperationsTrackingCode.ReasonArgument, nameof(OperationsHoldReasons.Refused), note));

        if (refused is not null)
        {
            return DecideOperationsDisposalResponse.Failure(refused);
        }

        _audit.Record(access.User.Id, request.Approve ? "ops.inspection.disposal_approved" : "ops.inspection.disposal_declined", nameof(OperationsInspection), inspection.Id,
            after: new { parcel = inspection.ParcelId, called = inspection.CalledCustomerUtc, note });

        if (await _unitOfWork.SaveChangesAsync(cancellationToken))
        {
            return DecideOperationsDisposalResponse.Success();
        }

        // The same decision sent twice at once: the other one was saved.
        _unitOfWork.DiscardChanges();
        return await _tracking.FindStoredByKeyAsync(key + ":decided", cancellationToken) is { } raced && raced.SubjectId == inspection.ParcelId
            ? DecideOperationsDisposalResponse.Success()
            : DecideOperationsDisposalResponse.Failure(OperationsCommonErrors.ConcurrencyConflict);
    }
}
