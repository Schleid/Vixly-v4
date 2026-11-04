using Vixly.Operations.Application.Audit;
using Vixly.Operations.Application.Ports;
using Vixly.Operations.Application.Receiving;
using Vixly.Operations.Application.Security;
using Vixly.Operations.Application.Shipments;
using Vixly.Operations.Domain.Access;
using Vixly.Operations.Domain.Common;
using Vixly.Operations.Domain.Locations;
using Vixly.Operations.Domain.Shipments;
using Vixly.Operations.Domain.Tracking;
using Vixly.Shared.Kernel.Domain;

namespace Vixly.Operations.Application.UseCases.Shipments.ChangeOperationsShipmentService;

// =========================================================
// FILE: ChangeOperationsShipmentServiceUseCase.cs
//
// PURPOSE:
// Staff change the service of a shipment before it leaves (Slice 5A; owner, Q-D1).
//
// RESPONSIBILITIES:
// - Run the rule and return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - Needs ops.shipments.manage for the origin warehouse
// - Only while the shipment is open, has instructions and every parcel is at the origin
// =========================================================
public sealed class ChangeOperationsShipmentServiceUseCase
{
    private readonly OperationsOwnerCheck _owner;

    private readonly OperationsReceivingScope _scope;

    private readonly IOperationsReceivingRepository _receiving;

    private readonly IOperationsTrackingRepository _tracking;

    private readonly OperationsShipmentIntake _intake;

    private readonly OperationsAuditTrail _audit;

    private readonly IOperationsUnitOfWork _unitOfWork;

    private readonly Vixly.Operations.Application.Insurance.OperationsCertificates _certificates;

    private readonly TimeProvider _timeProvider;

    private readonly OperationsShipmentBooking _booking;

    public ChangeOperationsShipmentServiceUseCase(
        OperationsOwnerCheck owner,
        OperationsReceivingScope scope,
        IOperationsReceivingRepository receiving,
        IOperationsTrackingRepository tracking,
        OperationsShipmentIntake intake,
        OperationsAuditTrail audit,
        IOperationsUnitOfWork unitOfWork,
        TimeProvider timeProvider,
        Vixly.Operations.Application.Insurance.OperationsCertificates certificates,
        OperationsShipmentBooking booking)
    {
        _booking = booking ?? throw new ArgumentNullException(nameof(booking));
        _certificates = certificates ?? throw new ArgumentNullException(nameof(certificates));
        _owner = owner ?? throw new ArgumentNullException(nameof(owner));
        _scope = scope ?? throw new ArgumentNullException(nameof(scope));
        _receiving = receiving ?? throw new ArgumentNullException(nameof(receiving));
        _tracking = tracking ?? throw new ArgumentNullException(nameof(tracking));
        _intake = intake ?? throw new ArgumentNullException(nameof(intake));
        _audit = audit ?? throw new ArgumentNullException(nameof(audit));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public async Task<ChangeOperationsShipmentServiceResponse> ExecuteAsync(
        OperationsAuthorizationContext context,
        ChangeOperationsShipmentServiceRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        if (await _owner.LoadAsync(context, cancellationToken) is not { } access)
        {
            return ChangeOperationsShipmentServiceResponse.Failure(OperationsAccessErrors.SignInRequired);
        }

        if (await _receiving.GetShipmentAsync(request.ShipmentId, cancellationToken) is not { } shipment
            || await _scope.CoveredAsync(access, OperationsPermissions.ShipmentsView, shipment.OriginWarehouseId, cancellationToken) is not { } origin)
        {
            return ChangeOperationsShipmentServiceResponse.Failure(OperationsShipmentErrors.NotFound);
        }

        if (!access.CoversWarehouse(OperationsPermissions.ShipmentsManage, origin.Id, origin.BranchId))
        {
            return ChangeOperationsShipmentServiceResponse.Failure(OperationsAccessErrors.PermissionRequired);
        }

        // A service the company prices for this customer, as the customer chooses it (Slice 4D).
        string code = request.ServiceCode?.Trim() ?? string.Empty;

        if (!(await _intake.ServicesAsync(shipment.CustomerId, cancellationToken)).Contains(code, StringComparer.Ordinal))
        {
            return ChangeOperationsShipmentServiceResponse.Failure(OperationsShipmentErrors.ServiceInvalid);
        }

        // The same service again (a double click): nothing to do.
        if (string.Equals(shipment.ServiceCode, code, StringComparison.Ordinal))
        {
            return ChangeOperationsShipmentServiceResponse.Success();
        }

        IReadOnlyList<OperationsParcel> parcels = await _receiving.ListShipmentParcelsAsync(shipment.Id, cancellationToken);

        if (parcels.Any(ShipmentParcels.Departed))
        {
            return ChangeOperationsShipmentServiceResponse.Failure(OperationsShipmentErrors.Departed);
        }

        // Insured under one mode's terms: another mode is the customer's choice to make (they accept its terms).
        if (shipment.Options.HasFlag(OperationsShipmentOptions.Insured)
            && Vixly.Operations.Domain.Charges.OperationsTransportModes.Of(shipment.ServiceCode) != Vixly.Operations.Domain.Charges.OperationsTransportModes.Of(code))
        {
            return ChangeOperationsShipmentServiceResponse.Failure(OperationsShipmentErrors.InsuredModeChange);
        }

        DateTimeOffset utcNow = _timeProvider.GetUtcNow();
        string? before = shipment.ServiceCode;

        try
        {
            shipment.ChangeService(code, utcNow);
        }
        catch (DomainException exception)
        {
            return ChangeOperationsShipmentServiceResponse.Failure(exception.Error);
        }

        // The shipping labels show the service: they go back to the labels to print (as a change by the customer, Slice 4D step 2).
        foreach (OperationsParcel listed in parcels)
        {
            if (await _tracking.GetParcelAsync(listed.Id, cancellationToken) is { } parcel)
            {
                parcel.ShippingLabelOutdated(utcNow);
            }
        }

        // Slice 5C (review): a service the company prices may be what the shipment was waiting for (C-2).
        await _booking.TryBookAsync(shipment, OperationsTrackingActor.Staff, access.User.Id, null, OperationsTrackingSource.Manual, utcNow, cancellationToken);

        _audit.Record(access.User.Id, "ops.shipments.service_changed", nameof(OperationsShipment), shipment.Id, before: new { service = before }, after: new { service = code });

        return await _unitOfWork.SaveChangesAsync(cancellationToken)
            ? ChangeOperationsShipmentServiceResponse.Success()
            : ChangeOperationsShipmentServiceResponse.Failure(OperationsCommonErrors.ConcurrencyConflict);
    }
}
