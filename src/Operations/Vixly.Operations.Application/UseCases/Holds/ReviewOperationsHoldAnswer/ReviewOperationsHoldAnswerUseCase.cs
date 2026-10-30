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

namespace Vixly.Operations.Application.UseCases.Holds.ReviewOperationsHoldAnswer;

// =========================================================
// FILE: ReviewOperationsHoldAnswerUseCase.cs
//
// PURPOSE:
// Staff accept a customer's answer to a hold, which ends it, or decline it with what is still missing (Slice 3E).
//
// RESPONSIBILITIES:
// - Run the rule and return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - Needs ops.holds.manage for the parcel's warehouse
// - Accepting a value answer corrects the parcel's declaration
// =========================================================
public sealed class ReviewOperationsHoldAnswerUseCase
{
    private readonly OperationsOwnerCheck _owner;

    private readonly OperationsReceivingScope _scope;

    private readonly IOperationsHoldRepository _holds;

    private readonly IOperationsTrackingRepository _tracking;

    private readonly OperationsInspectionSteps _steps;

    private readonly OperationsOutboxWriter _outbox;

    private readonly OperationsAuditTrail _audit;

    private readonly IOperationsUnitOfWork _unitOfWork;

    private readonly TimeProvider _timeProvider;

    public ReviewOperationsHoldAnswerUseCase(
        OperationsOwnerCheck owner,
        OperationsReceivingScope scope,
        IOperationsHoldRepository holds,
        IOperationsTrackingRepository tracking,
        OperationsInspectionSteps steps,
        OperationsOutboxWriter outbox,
        OperationsAuditTrail audit,
        IOperationsUnitOfWork unitOfWork,
        TimeProvider timeProvider)
    {
        _owner = owner ?? throw new ArgumentNullException(nameof(owner));
        _scope = scope ?? throw new ArgumentNullException(nameof(scope));
        _holds = holds ?? throw new ArgumentNullException(nameof(holds));
        _tracking = tracking ?? throw new ArgumentNullException(nameof(tracking));
        _steps = steps ?? throw new ArgumentNullException(nameof(steps));
        _outbox = outbox ?? throw new ArgumentNullException(nameof(outbox));
        _audit = audit ?? throw new ArgumentNullException(nameof(audit));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public async Task<ReviewOperationsHoldAnswerResponse> ExecuteAsync(
        OperationsAuthorizationContext context,
        ReviewOperationsHoldAnswerRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        if (await _owner.LoadAsync(context, cancellationToken) is not { } access)
        {
            return ReviewOperationsHoldAnswerResponse.Failure(OperationsAccessErrors.SignInRequired);
        }

        if (await _holds.GetAnswerAsync(request.AnswerId, cancellationToken) is not { } answer
            || await _tracking.GetParcelAsync(answer.ParcelId, cancellationToken) is not { WarehouseId: { } warehouseId } parcel
            || await _scope.CoveredAsync(access, OperationsPermissions.HoldsManage, warehouseId, cancellationToken) is not { } warehouse)
        {
            return ReviewOperationsHoldAnswerResponse.Failure(OperationsHoldErrors.AnswerNotFound);
        }

        if (!OperationsCommandKey.IsValid(request.CommandKey))
        {
            return ReviewOperationsHoldAnswerResponse.Failure(OperationsTrackingErrors.IdempotencyKeyInvalid);
        }

        string key = "ans:" + request.CommandKey;

        // Sent twice (a double click): this person's review is already recorded.
        if (answer.Status != OperationsHoldAnswerStatus.Open && answer.ReviewedByUserId == access.User.Id
            && (answer.Status == OperationsHoldAnswerStatus.Accepted) == request.Accept)
        {
            return ReviewOperationsHoldAnswerResponse.Success();
        }

        // The hold ended another way (released by hand, the parcel left or was decided): the answer has nothing left to decide.
        if (answer.Status == OperationsHoldAnswerStatus.Open
            && (!parcel.HoldReasons.HasFlag(answer.Reason) || parcel.Milestone is not (null or OperationsMilestone.ReceivedAtOrigin)))
        {
            return ReviewOperationsHoldAnswerResponse.Failure(OperationsHoldErrors.AnswerStale);
        }

        // A disposal the customer chose is still a disposal: only the Owner or an administrator accepts it (Q-C3).
        if (request.Accept && answer.Choice == "dispose" && !(access.IsOwner || access.IsAdministrator))
        {
            return ReviewOperationsHoldAnswerResponse.Failure(OperationsHoldErrors.ApproverNotAllowed);
        }

        DateTimeOffset utcNow = _timeProvider.GetUtcNow();

        try
        {
            if (request.Accept)
            {
                answer.Accept(access.User.Id, request.Note, utcNow);

                if (answer.Kind == OperationsHoldAnswerKind.Value)
                {
                    parcel.Declare(answer.Contents!, answer.DeclaredValue!.Value, answer.CurrencyCode!, utcNow);
                }
            }
            else
            {
                answer.Decline(access.User.Id, request.Note, utcNow);
            }
        }
        catch (DomainException exception)
        {
            return ReviewOperationsHoldAnswerResponse.Failure(exception.Error);
        }

        // Accepted: the hold ends; a return or a disposal the customer chose is the parcel's last milestone (ADR-0018).
        // Declined: the hold stays and the customer is told what is still missing.
        List<(string Key, string Code, string? Argument, string? Value, string? Note)> steps = new();

        if (request.Accept && answer.Choice is "return" or "dispose")
        {
            // The last milestone, then every hold ends with it, as for an unclaimed package (ADR-0018).
            steps.Add((key + ":final", answer.Choice == "return" ? OperationsTrackingCodes.ReturnedToSender : OperationsTrackingCodes.Disposed, null, null, answer.ReviewNote));

            foreach (OperationsHoldReasons reason in Enum.GetValues<OperationsHoldReasons>().Where(reason => reason != OperationsHoldReasons.None && parcel.HoldReasons.HasFlag(reason)))
            {
                steps.Add((key + ":release:" + reason, OperationsTrackingCodes.HoldReleased, OperationsTrackingCode.ReasonArgument, reason.ToString(), answer.ReviewNote));
            }
        }
        else if (request.Accept && parcel.HoldReasons.HasFlag(answer.Reason))
        {
            steps.Add((key + ":released", OperationsTrackingCodes.HoldReleased, OperationsTrackingCode.ReasonArgument, answer.Reason.ToString(), answer.ReviewNote));
        }

        if (steps.Count > 0 && await _steps.RecordAsync(parcel.Id, access, warehouse, utcNow, cancellationToken, steps.ToArray()) is { } refused)
        {
            return ReviewOperationsHoldAnswerResponse.Failure(refused);
        }

        if (!request.Accept)
        {
            _outbox.Write(OperationsReceivingMessages.ParcelNotice, context.RequiredTenantId, parcel.Id,
                new OperationsParcelNotice(answer.CustomerId, parcel.Id, Vixly.Operations.Application.Ports.OperationsCustomerMailKind.ParcelActionNeeded, request.PortalSlug));
        }

        _audit.Record(access.User.Id, request.Accept ? "ops.holds.answer_accepted" : "ops.holds.answer_declined", nameof(OperationsHoldAnswer), answer.Id,
            after: new { parcel = parcel.Number, reason = answer.Reason.ToString(), kind = answer.Kind.ToString(), choice = answer.Choice, note = answer.ReviewNote });

        if (await _unitOfWork.SaveChangesAsync(cancellationToken))
        {
            return ReviewOperationsHoldAnswerResponse.Success();
        }

        // Two reviews at once: the row version keeps the first; the same person's double click is that one.
        _unitOfWork.DiscardChanges();
        return await _holds.GetAnswerAsync(request.AnswerId, cancellationToken) is { ReviewedByUserId: { } reviewer } raced && reviewer == access.User.Id
            && (raced.Status == OperationsHoldAnswerStatus.Accepted) == request.Accept
            ? ReviewOperationsHoldAnswerResponse.Success()
            : ReviewOperationsHoldAnswerResponse.Failure(OperationsHoldErrors.AnswerNotOpen);
    }
}
