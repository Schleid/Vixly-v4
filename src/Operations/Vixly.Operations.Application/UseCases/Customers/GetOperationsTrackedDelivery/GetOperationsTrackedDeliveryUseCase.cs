using Vixly.Operations.Application.Delivery;
using Vixly.Operations.Application.Ports;
using Vixly.Operations.Application.Security;
using Vixly.Operations.Domain.Common;

namespace Vixly.Operations.Application.UseCases.Customers.GetOperationsTrackedDelivery;

// =========================================================
// FILE: GetOperationsTrackedDeliveryUseCase.cs
//
// PURPOSE:
// What the private link of an "out for delivery" notice shows (Slice 12C, owner H-5), without signing in.
//
// RESPONSIBILITIES:
// - Run the rule and return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - An unknown, old or other day's link answers "not found"; the shipment's number is the only detail shown
// =========================================================
public sealed class GetOperationsTrackedDeliveryUseCase
{
    private readonly IOperationsDriverTrackingRepository _tracking;

    private readonly IOperationsReceivingRepository _receiving;

    private readonly IOperationsSecretGenerator _secrets;

    private readonly OperationsDriverTracking _driver;

    public GetOperationsTrackedDeliveryUseCase(
        IOperationsDriverTrackingRepository tracking,
        IOperationsReceivingRepository receiving,
        IOperationsSecretGenerator secrets,
        OperationsDriverTracking driver)
    {
        _tracking = tracking ?? throw new ArgumentNullException(nameof(tracking));
        _receiving = receiving ?? throw new ArgumentNullException(nameof(receiving));
        _secrets = secrets ?? throw new ArgumentNullException(nameof(secrets));
        _driver = driver ?? throw new ArgumentNullException(nameof(driver));
    }

    public async Task<GetOperationsTrackedDeliveryResponse> ExecuteAsync(
        OperationsAuthorizationContext context,
        GetOperationsTrackedDeliveryRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        if (string.IsNullOrWhiteSpace(request.Token) || request.Token.Length > 64
            || await _tracking.FindByTrackTokenAsync(_secrets.HashToken(request.Token.Trim()), cancellationToken) is not { } found
            || await _receiving.GetShipmentAsync(found.Stop.ShipmentId, cancellationToken) is not { } shipment)
        {
            return GetOperationsTrackedDeliveryResponse.Failure(OperationsCommonErrors.NotFound);
        }

        // The link works on the route's day only; after it, it answers as unknown.
        return await _driver.WhereAsync(found.Stop, found.Route, shipment.Number ?? string.Empty, cancellationToken) is { } where
            ? GetOperationsTrackedDeliveryResponse.Success(where)
            : GetOperationsTrackedDeliveryResponse.Failure(OperationsCommonErrors.NotFound);
    }
}
