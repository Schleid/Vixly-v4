using Vixly.Operations.Application.Customers;
using Vixly.Operations.Application.Ports;
using Vixly.Operations.Application.Receiving;
using Vixly.Operations.Application.Security;
using Vixly.Operations.Application.Shipments;
using Vixly.Operations.Domain.Receiving;
using Vixly.Operations.Domain.Settings;
using Vixly.Operations.Domain.Shipments;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Shipments.CancelMyOperationsShipment;

// =========================================================
// FILE: CancelMyOperationsShipmentUseCase.cs
//
// PURPOSE:
// A customer cancels a shipment none of whose packages has arrived (owner, 2026-10-18).
//
// RESPONSIBILITIES:
// - Run the rule and return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - One use case per behaviour (Global Standard 5.1)
// =========================================================
public sealed class CancelMyOperationsShipmentUseCase
{
    private readonly OperationsCustomerAccounts _accounts;

    private readonly IOperationsReceivingRepository _receiving;

    private readonly IOperationsUnitOfWork _unitOfWork;

    private readonly Vixly.Operations.Application.Insurance.OperationsCertificates _certificates;

    private readonly Vixly.Operations.Application.Supplies.OperationsSupplySales _supplies;

    public CancelMyOperationsShipmentUseCase(
        OperationsCustomerAccounts accounts,
        IOperationsReceivingRepository receiving,
        IOperationsUnitOfWork unitOfWork,
        Vixly.Operations.Application.Insurance.OperationsCertificates certificates,
        Vixly.Operations.Application.Supplies.OperationsSupplySales supplies)
    {
        _certificates = certificates ?? throw new ArgumentNullException(nameof(certificates));
        _supplies = supplies ?? throw new ArgumentNullException(nameof(supplies));
        _accounts = accounts ?? throw new ArgumentNullException(nameof(accounts));
        _receiving = receiving ?? throw new ArgumentNullException(nameof(receiving));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
    }

    public async Task<CancelMyOperationsShipmentResponse> ExecuteAsync(
        OperationsAuthorizationContext context,
        CancelMyOperationsShipmentRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        if (await _accounts.SignedInAsync(request.SessionToken, cancellationToken) is not { } signedIn)
        {
            return CancelMyOperationsShipmentResponse.Failure(OperationsCustomerAuthErrors.SessionRequired);
        }

        // Only the customer's own shipment; another customer's answers "not found" (S-3).
        if (await _receiving.GetShipmentAsync(request.ShipmentId, cancellationToken) is not { } shipment || shipment.CustomerId != signedIn.Customer.Id)
        {
            return CancelMyOperationsShipmentResponse.Failure(OperationsShipmentErrors.NotFound);
        }

        // Cancelled already (a double click): the same answer.
        if (shipment.Status == OperationsShipmentStatus.Cancelled)
        {
            return CancelMyOperationsShipmentResponse.Success(shipment.Id);
        }

        if ((await _receiving.ListShipmentParcelsAsync(shipment.Id, cancellationToken)).Count > 0)
        {
            return CancelMyOperationsShipmentResponse.Failure(OperationsShipmentErrors.HasParcels);
        }

        DateTimeOffset utcNow = _accounts.UtcNow;

        try
        {
            shipment.Cancel(utcNow);

            // Slice 4D: its pre-alerts stay; they are in no shipment again, for the customer to place in another (owner, 2026-10-19).
            foreach (OperationsPreAlert preAlert in (await _receiving.ListShipmentPreAlertsAsync(shipment.Id, cancellationToken)).Where(preAlert => preAlert.Status == OperationsPreAlertStatus.Open))
            {
                preAlert.TakeOut(utcNow);
            }
        }
        catch (Vixly.Shared.Kernel.Domain.DomainException exception)
        {
            return CancelMyOperationsShipmentResponse.Failure(exception.Error);
        }

        // Slice 5B: the insurance certificate follows what the customer chose.
        await _certificates.SyncAsync(shipment, utcNow, cancellationToken);

        // Slice 5E: supplies added at the counter go with it (never sold: the shipment was open).
        await _supplies.CancelForShipmentAsync(shipment, null, utcNow, cancellationToken);

        return await _unitOfWork.SaveChangesAsync(cancellationToken)
            ? CancelMyOperationsShipmentResponse.Success(shipment.Id)
            : CancelMyOperationsShipmentResponse.Failure(Vixly.Operations.Domain.Common.OperationsCommonErrors.ConcurrencyConflict);
    }
}
