using Vixly.Operations.Application.Customers;
using Vixly.Operations.Application.Ports;
using Vixly.Operations.Application.Receiving;
using Vixly.Operations.Application.Security;
using Vixly.Operations.Domain.Receiving;
using Vixly.Operations.Domain.Tracking;
using Vixly.Shared.Kernel.Domain;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Receiving.CancelMyOperationsPreAlert;

// =========================================================
// FILE: CancelMyOperationsPreAlertUseCase.cs
//
// PURPOSE:
// A customer withdraws a pre-alert that has not been received (Slice 3B).
//
// RESPONSIBILITIES:
// - Run the rule and return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - Another customer's pre-alert gives the same answer as a missing one (S-3)
// =========================================================
public sealed class CancelMyOperationsPreAlertUseCase
{
    private readonly OperationsCustomerAccounts _accounts;

    private readonly IOperationsReceivingRepository _receiving;

    private readonly IOperationsUnitOfWork _unitOfWork;

    private readonly Vixly.Operations.Application.Shipments.OperationsShipmentBooking _booking;

    public CancelMyOperationsPreAlertUseCase(
        OperationsCustomerAccounts accounts,
        IOperationsReceivingRepository receiving,
        IOperationsUnitOfWork unitOfWork,
        Vixly.Operations.Application.Shipments.OperationsShipmentBooking booking)
    {
        _booking = booking ?? throw new ArgumentNullException(nameof(booking));
        _accounts = accounts ?? throw new ArgumentNullException(nameof(accounts));
        _receiving = receiving ?? throw new ArgumentNullException(nameof(receiving));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
    }

    public async Task<CancelMyOperationsPreAlertResponse> ExecuteAsync(
        OperationsAuthorizationContext context,
        CancelMyOperationsPreAlertRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        if (await _accounts.SignedInAsync(request.SessionToken, cancellationToken) is not { } signedIn)
        {
            return CancelMyOperationsPreAlertResponse.Failure(OperationsCustomerAuthErrors.SessionRequired);
        }

        if (await _receiving.GetPreAlertAsync(request.PreAlertId, cancellationToken) is not { } preAlert || preAlert.CustomerId != signedIn.Customer.Id)
        {
            return CancelMyOperationsPreAlertResponse.Failure(OperationsReceivingErrors.PreAlertNotFound);
        }

        try
        {
            preAlert.Cancel(_accounts.UtcNow);
        }
        catch (DomainException exception)
        {
            return CancelMyOperationsPreAlertResponse.Failure(exception.Error);
        }

        // Slice 5C (review): the package withdrawn may be the last thing its shipment waited for (C-2).
        if (preAlert.ShipmentId is { } shipmentId && await _receiving.GetShipmentAsync(shipmentId, cancellationToken) is { } shipment)
        {
            await _booking.TryBookAsync(shipment, OperationsTrackingActor.Customer, null, null, OperationsTrackingSource.Customer, _accounts.UtcNow, cancellationToken);
        }

        return await _unitOfWork.SaveChangesAsync(cancellationToken)
            ? CancelMyOperationsPreAlertResponse.Success(preAlert.Id)
            : CancelMyOperationsPreAlertResponse.Failure(OperationsReceivingErrors.PreAlertNotOpen);
    }
}
