using Vixly.Operations.Application.Customers;
using Vixly.Operations.Application.Ports;
using Vixly.Operations.Application.Receiving;
using Vixly.Operations.Application.Security;
using Vixly.Operations.Domain.Receiving;
using Vixly.Operations.Domain.Settings;
using Vixly.Operations.Domain.Tracking;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Receiving.ListMyOperationsPackages;

// =========================================================
// FILE: ListMyOperationsPackagesUseCase.cs
//
// PURPOSE:
// A customer's pre-alerts and packages, newest first (Slice 3B).
//
// RESPONSIBILITIES:
// - Run the rule and return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - Only the signed-in customer's own records (S-3)
// =========================================================
public sealed class ListMyOperationsPackagesUseCase
{
    private readonly OperationsCustomerAccounts _accounts;

    private readonly IOperationsReceivingRepository _receiving;

    private readonly IOperationsLocationRepository _locations;

    private readonly IOperationsSettingsRepository _settings;

    public ListMyOperationsPackagesUseCase(
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

    public async Task<ListMyOperationsPackagesResponse> ExecuteAsync(
        OperationsAuthorizationContext context,
        ListMyOperationsPackagesRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        if (await _accounts.SignedInAsync(request.SessionToken, cancellationToken) is not { } signedIn)
        {
            return ListMyOperationsPackagesResponse.Failure(OperationsCustomerAuthErrors.SessionRequired);
        }

        IReadOnlyList<OperationsPreAlertView> preAlerts = (await _receiving.ListOpenPreAlertsAsync(signedIn.Customer.Id, cancellationToken))
            .OrderByDescending(preAlert => preAlert.CreatedUtc)
            .Select(OperationsPreAlertView.Of)
            .ToList();
        Dictionary<Guid, string> warehouses = (await _locations.ListWarehousesAsync(cancellationToken)).ToDictionary(warehouse => warehouse.Id, warehouse => warehouse.Name);
        IReadOnlyList<OperationsCustomerParcel> parcels = (await _receiving.ListCustomerParcelsAsync(signedIn.Customer.Id, cancellationToken))
            .OrderByDescending(parcel => parcel.CreatedUtc)
            .Select(parcel => OperationsCustomerParcel.Of(parcel, parcel.WarehouseId is { } id ? warehouses.GetValueOrDefault(id) : null))
            .ToList();
        Vixly.Operations.Domain.Settings.OperationsTenantSettings? settings = await _settings.FindAsync(cancellationToken);

        // The customer's shipments with how many packages were announced and arrived (owner, 2026-10-18).
        Dictionary<Guid, string> branches = (await _locations.ListBranchesAsync(cancellationToken)).ToDictionary(branch => branch.Id, branch => branch.Name);
        IReadOnlyList<Vixly.Operations.Application.Shipments.OperationsCustomerShipment> shipments = (await _receiving.ListCustomerShipmentsAsync(signedIn.Customer.Id, cancellationToken))
            .Select(shipment => Vixly.Operations.Application.Shipments.OperationsCustomerShipment.Of(shipment, warehouses.GetValueOrDefault(shipment.OriginWarehouseId),
                shipment.DestinationWarehouseId is { } destinationId ? warehouses.GetValueOrDefault(destinationId)
                    : shipment.DestinationBranchId is { } branchId ? branches.GetValueOrDefault(branchId) : null,
                preAlerts.Count(preAlert => preAlert.ShipmentId == shipment.Id), parcels.Count(parcel => parcel.ShipmentId == shipment.Id)))
            .ToList();

        return ListMyOperationsPackagesResponse.Success(preAlerts, parcels, settings?.WeightUnit ?? OperationsWeightUnit.Kilogram, settings?.DimensionUnit ?? OperationsDimensionUnit.Centimetre, shipments);
    }
}
