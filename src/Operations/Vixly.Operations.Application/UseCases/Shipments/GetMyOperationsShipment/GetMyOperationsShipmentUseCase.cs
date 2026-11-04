using Vixly.Operations.Application.Customers;
using Vixly.Operations.Application.Ports;
using Vixly.Operations.Application.Receiving;
using Vixly.Operations.Application.Security;
using Vixly.Operations.Application.Shipments;
using Vixly.Operations.Domain.Receiving;
using Vixly.Operations.Domain.Settings;
using Vixly.Operations.Domain.Shipments;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Shipments.GetMyOperationsShipment;

// =========================================================
// FILE: GetMyOperationsShipmentUseCase.cs
//
// PURPOSE:
// A customer's shipment with its announced and arrived packages (owner, 2026-10-18).
//
// RESPONSIBILITIES:
// - Run the rule and return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - One use case per behaviour (Global Standard 5.1)
// =========================================================
public sealed class GetMyOperationsShipmentUseCase
{
    private readonly OperationsCustomerAccounts _accounts;

    private readonly IOperationsReceivingRepository _receiving;

    private readonly IOperationsLocationRepository _locations;

    private readonly IOperationsSettingsRepository _settings;

    public GetMyOperationsShipmentUseCase(
        OperationsCustomerAccounts accounts,
        IOperationsReceivingRepository receiving,
        IOperationsLocationRepository locations,
        IOperationsSettingsRepository settings)
    {
        _accounts = accounts ?? throw new ArgumentNullException(nameof(accounts));
        _receiving = receiving ?? throw new ArgumentNullException(nameof(receiving));
        _locations = locations ?? throw new ArgumentNullException(nameof(locations));
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
    }

    public async Task<GetMyOperationsShipmentResponse> ExecuteAsync(
        OperationsAuthorizationContext context,
        GetMyOperationsShipmentRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        if (await _accounts.SignedInAsync(request.SessionToken, cancellationToken) is not { } signedIn)
        {
            return GetMyOperationsShipmentResponse.Failure(OperationsCustomerAuthErrors.SessionRequired);
        }

        // Only the customer's own shipment; another customer's answers "not found" (S-3).
        if (await _receiving.GetShipmentAsync(request.ShipmentId, cancellationToken) is not { } shipment || shipment.CustomerId != signedIn.Customer.Id)
        {
            return GetMyOperationsShipmentResponse.Failure(OperationsShipmentErrors.NotFound);
        }

        IReadOnlyList<OperationsPreAlert> preAlerts = await _receiving.ListShipmentPreAlertsAsync(shipment.Id, cancellationToken);
        IReadOnlyList<Vixly.Operations.Domain.Tracking.OperationsParcel> parcels = await _receiving.ListShipmentParcelsAsync(shipment.Id, cancellationToken);
        string? origin = (await _locations.GetWarehouseAsync(shipment.OriginWarehouseId, cancellationToken))?.Name;
        // Slice 4C: picked up at the destination warehouse the customer chose; before 4C, at a destination branch.
        string? branch = shipment.DestinationWarehouseId is { } destinationId ? (await _locations.GetWarehouseAsync(destinationId, cancellationToken))?.Name
            : shipment.DestinationBranchId is { } branchId ? (await _locations.GetBranchAsync(branchId, cancellationToken))?.Name : null;
        OperationsTenantSettings? settings = await _settings.FindAsync(cancellationToken);

        return GetMyOperationsShipmentResponse.Success(
            OperationsCustomerShipment.Of(shipment, origin, branch, preAlerts.Count(preAlert => preAlert.Status == OperationsPreAlertStatus.Open), parcels.Count),
            preAlerts.Where(preAlert => preAlert.Status == OperationsPreAlertStatus.Open).Select(OperationsPreAlertView.Of).ToList(),
            parcels.Select(parcel => OperationsCustomerParcel.Of(parcel, origin)).ToList(),
            shipment.Status == OperationsShipmentStatus.Open && parcels.Count == 0,
            settings?.WeightUnit ?? OperationsWeightUnit.Kilogram,
            settings?.DimensionUnit ?? OperationsDimensionUnit.Centimetre);
    }
}
