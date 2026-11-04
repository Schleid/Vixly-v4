using Vixly.Operations.Application.Arrivals;
using Vixly.Operations.Application.Audit;
using Vixly.Operations.Application.Billing;
using Vixly.Operations.Application.Bundles;
using Vixly.Operations.Application.Customers;
using Vixly.Operations.Application.Pickup;
using Vixly.Operations.Application.Ports;
using Vixly.Operations.Application.Security;
using Vixly.Operations.Application.Tracking;
using Vixly.Operations.Domain.Access;
using Vixly.Operations.Domain.Billing;
using Vixly.Operations.Domain.Charges;
using Vixly.Operations.Domain.Common;
using Vixly.Operations.Domain.Locations;
using Vixly.Operations.Domain.Pickup;
using Vixly.Operations.Domain.Settings;
using Vixly.Operations.Domain.Shipments;
using Vixly.Operations.Domain.Tracking;
using Vixly.Shared.Kernel.Domain;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Pickup.ChangeMyOperationsCollector;

// =========================================================
// FILE: ChangeMyOperationsCollectorUseCase.cs
//
// PURPOSE:
// The customer names who may collect a shipment, or no one (Slice 7 step 2, R-6).
//
// RESPONSIBILITIES:
// - Run the rule and return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - The customer's own booked shipment, until it is collected
// - Same rules as the recipient: a name and a phone in international form
// =========================================================
public sealed class ChangeMyOperationsCollectorUseCase
{
    private readonly OperationsCustomerAccounts _accounts;

    private readonly IOperationsReceivingRepository _receiving;

    private readonly IOperationsUnitOfWork _unitOfWork;

    public ChangeMyOperationsCollectorUseCase(
        OperationsCustomerAccounts accounts,
        IOperationsReceivingRepository receiving,
        IOperationsUnitOfWork unitOfWork)
    {
        _accounts = accounts ?? throw new ArgumentNullException(nameof(accounts));
        _receiving = receiving ?? throw new ArgumentNullException(nameof(receiving));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
    }

    public async Task<ChangeMyOperationsCollectorResponse> ExecuteAsync(
        OperationsAuthorizationContext context,
        ChangeMyOperationsCollectorRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);
        if (await _accounts.SignedInAsync(request.SessionToken, cancellationToken) is not { } signedIn)
        {
            return ChangeMyOperationsCollectorResponse.Failure(OperationsCustomerAuthErrors.SessionRequired);
        }

        // Only the customer's own shipment; another customer's answers "not found" (S-3).
        if (await _receiving.GetShipmentAsync(request.ShipmentId, cancellationToken) is not { } shipment || shipment.CustomerId != signedIn.Customer.Id)
        {
            return ChangeMyOperationsCollectorResponse.Failure(OperationsShipmentErrors.NotFound);
        }

        try
        {
            shipment.ChangeCollector(request.Name, request.Phone, _accounts.UtcNow);
        }
        catch (DomainException exception)
        {
            return ChangeMyOperationsCollectorResponse.Failure(exception.Error);
        }

        if (await _unitOfWork.SaveChangesAsync(cancellationToken))
        {
            return ChangeMyOperationsCollectorResponse.Success();
        }

        _unitOfWork.DiscardChanges();
        return ChangeMyOperationsCollectorResponse.Failure(OperationsCommonErrors.ConcurrencyConflict);
    }
}
