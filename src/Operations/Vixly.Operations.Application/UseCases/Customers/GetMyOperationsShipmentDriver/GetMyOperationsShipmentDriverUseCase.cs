using Vixly.Operations.Application.Customers;
using Vixly.Operations.Application.Delivery;
using Vixly.Operations.Application.Ports;
using Vixly.Operations.Application.Security;
using Vixly.Operations.Domain.Shipments;

namespace Vixly.Operations.Application.UseCases.Customers.GetMyOperationsShipmentDriver;

// =========================================================
// FILE: GetMyOperationsShipmentDriverUseCase.cs
//
// PURPOSE:
// Where the driver bringing a customer's shipment is (Slice 12C, owner H-5): the stops before theirs, and the place once theirs is next.
//
// RESPONSIBILITIES:
// - Run the rule and return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - Another customer's shipment answers "not found" (S-3)
// - Nothing is shown off the route's day or once the route is back
// =========================================================
public sealed class GetMyOperationsShipmentDriverUseCase
{
    private readonly OperationsCustomerAccounts _accounts;

    private readonly IOperationsReceivingRepository _receiving;

    private readonly IOperationsDriverTrackingRepository _tracking;

    private readonly OperationsDriverTracking _driver;

    public GetMyOperationsShipmentDriverUseCase(
        OperationsCustomerAccounts accounts,
        IOperationsReceivingRepository receiving,
        IOperationsDriverTrackingRepository tracking,
        OperationsDriverTracking driver)
    {
        _accounts = accounts ?? throw new ArgumentNullException(nameof(accounts));
        _receiving = receiving ?? throw new ArgumentNullException(nameof(receiving));
        _tracking = tracking ?? throw new ArgumentNullException(nameof(tracking));
        _driver = driver ?? throw new ArgumentNullException(nameof(driver));
    }

    public async Task<GetMyOperationsShipmentDriverResponse> ExecuteAsync(
        OperationsAuthorizationContext context,
        GetMyOperationsShipmentDriverRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        if (await _accounts.SignedInAsync(request.SessionToken, cancellationToken) is not { } signedIn)
        {
            return GetMyOperationsShipmentDriverResponse.Failure(OperationsCustomerAuthErrors.SessionRequired);
        }

        // Only the customer's own shipment; another customer's answers "not found" (S-3).
        if (await _receiving.GetShipmentAsync(request.ShipmentId, cancellationToken) is not { } shipment || shipment.CustomerId != signedIn.Customer.Id)
        {
            return GetMyOperationsShipmentDriverResponse.Failure(OperationsShipmentErrors.NotFound);
        }

        // Not out for delivery today: nothing to show (a success without a driver).
        return await _tracking.FindOutStopAsync(shipment.Id, cancellationToken) is { } found
            ? GetMyOperationsShipmentDriverResponse.Success(await _driver.WhereAsync(found.Stop, found.Route, shipment.Number ?? string.Empty, cancellationToken))
            : GetMyOperationsShipmentDriverResponse.Success(null);
    }
}
