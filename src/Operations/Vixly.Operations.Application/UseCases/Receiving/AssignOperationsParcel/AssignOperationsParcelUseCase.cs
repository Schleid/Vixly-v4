using Vixly.Operations.Application.Audit;
using Vixly.Operations.Application.Messaging;
using Vixly.Operations.Application.Ports;
using Vixly.Operations.Application.Receiving;
using Vixly.Operations.Application.Security;
using Vixly.Operations.Application.Shipments;
using Vixly.Operations.Application.Tracking;
using Vixly.Operations.Domain.Access;
using Vixly.Operations.Domain.Common;
using Vixly.Operations.Domain.Customers;
using Vixly.Operations.Domain.Locations;
using Vixly.Operations.Domain.Receiving;
using Vixly.Operations.Domain.Settings;
using Vixly.Operations.Domain.Shipments;
using Vixly.Operations.Domain.Tracking;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Receiving.AssignOperationsParcel;

// =========================================================
// FILE: AssignOperationsParcelUseCase.cs
//
// PURPOSE:
// Gives an unidentified parcel to the customer who proved it is theirs (Slice 3B, owner Q-R1).
//
// RESPONSIBILITIES:
// - Run the rule and return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - Needs ops.parcels.assign for the parcel's warehouse
// - The parcel joins the customer's open shipment at that warehouse (D-R1)
// =========================================================
public sealed class AssignOperationsParcelUseCase
{
    private readonly OperationsOwnerCheck _owner;

    private readonly OperationsReceivingScope _scope;

    private readonly IOperationsReceivingRepository _receiving;

    private readonly IOperationsTrackingRepository _tracking;

    private readonly IOperationsCustomerRepository _customers;

    private readonly OperationsTracker _tracker;

    private readonly OperationsShipmentPlacement _placement;

    private readonly OperationsOutboxWriter _outbox;

    private readonly OperationsAuditTrail _audit;

    private readonly IOperationsUnitOfWork _unitOfWork;

    private readonly TimeProvider _timeProvider;

    public AssignOperationsParcelUseCase(
        OperationsOwnerCheck owner,
        OperationsReceivingScope scope,
        IOperationsReceivingRepository receiving,
        IOperationsTrackingRepository tracking,
        IOperationsCustomerRepository customers,
        OperationsTracker tracker,
        OperationsShipmentPlacement placement,
        OperationsOutboxWriter outbox,
        OperationsAuditTrail audit,
        IOperationsUnitOfWork unitOfWork,
        TimeProvider timeProvider)
    {
        _owner = owner ?? throw new ArgumentNullException(nameof(owner));
        _scope = scope ?? throw new ArgumentNullException(nameof(scope));
        _receiving = receiving ?? throw new ArgumentNullException(nameof(receiving));
        _tracking = tracking ?? throw new ArgumentNullException(nameof(tracking));
        _customers = customers ?? throw new ArgumentNullException(nameof(customers));
        _tracker = tracker ?? throw new ArgumentNullException(nameof(tracker));
        _placement = placement ?? throw new ArgumentNullException(nameof(placement));
        _outbox = outbox ?? throw new ArgumentNullException(nameof(outbox));
        _audit = audit ?? throw new ArgumentNullException(nameof(audit));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public async Task<AssignOperationsParcelResponse> ExecuteAsync(
        OperationsAuthorizationContext context,
        AssignOperationsParcelRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        if (await _owner.LoadAsync(context, cancellationToken) is not { } access)
        {
            return AssignOperationsParcelResponse.Failure(OperationsAccessErrors.SignInRequired);
        }

        if (await _tracking.GetParcelAsync(request.ParcelId, cancellationToken) is not { WarehouseId: { } warehouseId } parcel
            || await _scope.CoveredAsync(access, OperationsPermissions.ParcelsView, warehouseId, cancellationToken) is not { } warehouse)
        {
            return AssignOperationsParcelResponse.Failure(OperationsReceivingErrors.ParcelNotFound);
        }

        if (!access.CoversWarehouse(OperationsPermissions.ParcelsAssign, warehouse.Id, warehouse.BranchId))
        {
            return AssignOperationsParcelResponse.Failure(OperationsAccessErrors.PermissionRequired);
        }

        if (!OperationsCommandKey.IsValid(request.CommandKey))
        {
            return AssignOperationsParcelResponse.Failure(OperationsTrackingErrors.IdempotencyKeyInvalid);
        }

        string key = "asg:" + request.CommandKey;

        if (await _tracking.FindStoredByKeyAsync(key + ":assigned", cancellationToken) is { } earlier && earlier.SubjectId == parcel.Id)
        {
            return AssignOperationsParcelResponse.Success(parcel.Id);
        }

        if (parcel.IsIdentified || !parcel.HoldReasons.HasFlag(OperationsHoldReasons.Unidentified))
        {
            return AssignOperationsParcelResponse.Failure(OperationsReceivingErrors.NotUnidentified);
        }

        // The clerk records what the customer proved; a name alone is never enough (D-R8).
        if (!OperationsTrackingCodes.VerificationMethods.Contains(request.VerifiedBy, StringComparer.Ordinal))
        {
            return AssignOperationsParcelResponse.Failure(OperationsReceivingErrors.VerificationRequired);
        }

        if (string.IsNullOrWhiteSpace(request.CustomerCode)
            || await _customers.FindByCodeAsync(request.CustomerCode.Trim().ToUpperInvariant(), cancellationToken) is not { Status: OperationsCustomerStatus.Active, Code: { } code } customer)
        {
            return AssignOperationsParcelResponse.Failure(OperationsReceivingErrors.CustomerNotFound);
        }

        // Slice 4D step 3 (review): "the mailbox code on the label" only when the recorded label carries this customer's code.
        if (request.VerifiedBy == "label_code" && parcel.LabelCode != code)
        {
            return AssignOperationsParcelResponse.Failure(OperationsReceivingErrors.VerificationRequired);
        }

        DateTimeOffset utcNow = _timeProvider.GetUtcNow();

        // The customer may have announced it after it arrived: their declaration is used, and it joins the shipment the pre-alert is in, or waits
        // for the customer to place it (Slice 4D).
        OperationsPreAlert? preAlert = parcel.CarrierTrackingNumber is { } tracking ? await _receiving.FindOpenPreAlertAsync(customer.Id, tracking, cancellationToken) : null;
        (OperationsShipment shipment, bool placed) = await _placement.PlaceAsync(context.RequiredTenantId, customer.Id, warehouse.Id, preAlert, utcNow, cancellationToken);
        parcel.JoinShipment(shipment, utcNow);

        if (preAlert is not null)
        {
            parcel.MatchPreAlert(preAlert, utcNow);

            if (!placed)
            {
                preAlert.TakeOut(utcNow);
            }
        }

        List<OperationsTrackingDraft> steps = new()
        {
            Step(parcel, OperationsTrackingCodes.CustomerAssigned, key + ":assigned", access, warehouse, utcNow, ("customer", code), ("verified_by", request.VerifiedBy)),
            Step(parcel, OperationsTrackingCodes.HoldReleased, key + ":release", access, warehouse, utcNow, ("reason", nameof(OperationsHoldReasons.Unidentified)))
        };

        if (preAlert is null)
        {
            steps.Add(Step(parcel, OperationsTrackingCodes.HoldOpened, key + ":hold:information", access, warehouse, utcNow, ("reason", nameof(OperationsHoldReasons.AwaitingCustomerInformation))));
        }

        // As at the dock: a pre-alert in none of the customer's shipments waits for one; without a pre-alert, the customer declares it first.
        if (preAlert is not null && !placed)
        {
            steps.Add(Step(parcel, OperationsTrackingCodes.HoldOpened, key + ":hold:shipment", access, warehouse, utcNow, ("reason", nameof(OperationsHoldReasons.AwaitingShipment))));
        }

        foreach (OperationsTrackingDraft step in steps)
        {
            if ((await _tracker.RecordAsync(step, cancellationToken)).Error is { } refused)
            {
                // Slice 4D step 4 (review): nothing of the half-done assignment may be saved by a later call in the same request.
                _unitOfWork.DiscardChanges();
                return AssignOperationsParcelResponse.Failure(refused);
            }
        }

        _outbox.Write(OperationsReceivingMessages.ParcelNotice, context.RequiredTenantId, parcel.Id,
            new OperationsParcelNotice(customer.Id, parcel.Id, preAlert is null || !placed ? OperationsCustomerMailKind.ParcelActionNeeded : OperationsCustomerMailKind.ParcelReceived, request.PortalSlug));
        _audit.Record(access.User.Id, "ops.parcels.assigned", nameof(OperationsParcel), parcel.Id, after: new { number = parcel.Number, customer = code, verifiedBy = request.VerifiedBy });

        if (await _unitOfWork.SaveChangesAsync(cancellationToken))
        {
            return AssignOperationsParcelResponse.Success(parcel.Id);
        }

        // The same form sent twice at once: the other request saved it, so this one is a repeat (ADR-0019, review M2).
        _unitOfWork.DiscardChanges();
        return await _tracking.FindStoredByKeyAsync(key + ":assigned", cancellationToken) is { } raced && raced.SubjectId == parcel.Id
            ? AssignOperationsParcelResponse.Success(parcel.Id)
            : AssignOperationsParcelResponse.Failure(OperationsCommonErrors.ConcurrencyConflict);
    }

    private static OperationsTrackingDraft Step(OperationsParcel parcel, string code, string key, OperationsAccessView access, OperationsWarehouse warehouse, DateTimeOffset utcNow,
        params (string Name, string Value)[] arguments) =>
        new(OperationsTrackingSubject.Parcel, parcel.Id, code, arguments.ToDictionary(item => item.Name, item => item.Value, StringComparer.Ordinal), utcNow, key,
            OperationsTrackingActor.Staff, OperationsTrackingSource.Manual)
        {
            ActorUserId = access.User.Id,
            LocationCode = warehouse.Code
        };
    
}
