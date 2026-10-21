using Vixly.Operations.Application.Customers;
using Vixly.Operations.Application.Ports;
using Vixly.Operations.Application.Security;
using Vixly.Operations.Application.Shipments;
using Vixly.Operations.Domain.Common;
using Vixly.Operations.Domain.Shipments;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Shipments.PlaceMyOperationsPackage;

// =========================================================
// FILE: PlaceMyOperationsPackageUseCase.cs
//
// PURPOSE:
// A customer puts one of its packages into one of its shipments, moves it, or takes it out (Slice 4D; owner, 2026-10-19).
//
// RESPONSIBILITIES:
// - Run the rule and return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - Another customer's package or shipment gives the same answer as a missing one (S-3)
// - A parcel moves only while it is at its origin
// =========================================================
public sealed class PlaceMyOperationsPackageUseCase
{
    private readonly OperationsCustomerAccounts _accounts;

    private readonly IOperationsReceivingRepository _receiving;

    private readonly OperationsPackageMover _mover;

    private readonly IOperationsUnitOfWork _unitOfWork;

    private readonly OperationsShipmentBooking _booking;

    public PlaceMyOperationsPackageUseCase(
        OperationsCustomerAccounts accounts,
        IOperationsReceivingRepository receiving,
        OperationsPackageMover mover,
        IOperationsUnitOfWork unitOfWork,
        OperationsShipmentBooking booking)
    {
        _booking = booking ?? throw new ArgumentNullException(nameof(booking));
        _accounts = accounts ?? throw new ArgumentNullException(nameof(accounts));
        _receiving = receiving ?? throw new ArgumentNullException(nameof(receiving));
        _mover = mover ?? throw new ArgumentNullException(nameof(mover));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
    }

    public async Task<PlaceMyOperationsPackageResponse> ExecuteAsync(
        OperationsAuthorizationContext context,
        PlaceMyOperationsPackageRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        if (await _accounts.SignedInAsync(request.SessionToken, cancellationToken) is not { } signedIn)
        {
            return PlaceMyOperationsPackageResponse.Failure(OperationsCustomerAuthErrors.SessionRequired);
        }

        // Only into the customer's own open shipment with instructions.
        OperationsShipment? target = request.ShipmentId is { } shipmentId ? await _receiving.GetShipmentAsync(shipmentId, cancellationToken) : null;

        if (request.ShipmentId is not null && (target is null || target.CustomerId != signedIn.Customer.Id))
        {
            return PlaceMyOperationsPackageResponse.Failure(OperationsShipmentErrors.NotFound);
        }

        if (target is { HasInstructions: false })
        {
            return PlaceMyOperationsPackageResponse.Failure(OperationsShipmentErrors.InstructionsRequired);
        }

        // The shipment an announced package leaves: it may have waited for nothing else.
        Guid? left = request.PreAlertId is { } preAlertId ? (await _receiving.GetPreAlertAsync(preAlertId, cancellationToken))?.ShipmentId : null;

        if (await _mover.PlaceForCustomerAsync(signedIn.Customer.Id, request.PreAlertId, request.ParcelId, target, _accounts.UtcNow, cancellationToken) is { } refused)
        {
            return PlaceMyOperationsPackageResponse.Failure(refused);
        }

        // Slice 5C (review): the shipment it joins, and the one it left, may now hold all their parcels (C-2).
        foreach (OperationsShipment shipment in new[] { target, left is { } id && id != target?.Id ? await _receiving.GetShipmentAsync(id, cancellationToken) : null }.OfType<OperationsShipment>())
        {
            await _booking.TryBookAsync(shipment, Vixly.Operations.Domain.Tracking.OperationsTrackingActor.Customer, null, null, Vixly.Operations.Domain.Tracking.OperationsTrackingSource.Customer,
                _accounts.UtcNow, cancellationToken);
        }

        return await _unitOfWork.SaveChangesAsync(cancellationToken)
            ? PlaceMyOperationsPackageResponse.Success()
            : PlaceMyOperationsPackageResponse.Failure(OperationsCommonErrors.ConcurrencyConflict);
    }
}
