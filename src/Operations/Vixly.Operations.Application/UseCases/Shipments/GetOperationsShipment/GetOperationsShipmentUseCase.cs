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

namespace Vixly.Operations.Application.UseCases.Shipments.GetOperationsShipment;

// =========================================================
// FILE: GetOperationsShipmentUseCase.cs
//
// PURPOSE:
// One shipment for staff: where it goes, its customer, its pre-alerts and each parcel with where it stands (Slice 5A).
//
// RESPONSIBILITIES:
// - Run the rule and return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - Needs ops.shipments.view for the origin warehouse; the customer's name only for staff who may see customers
// =========================================================
public sealed class GetOperationsShipmentUseCase
{
    private readonly OperationsOwnerCheck _owner;

    private readonly OperationsReceivingScope _scope;

    private readonly IOperationsReceivingRepository _receiving;

    private readonly IOperationsCustomerRepository _customers;

    private readonly IOperationsLocationRepository _locations;

    private readonly IOperationsShippingServiceRepository _services;

    private readonly IOperationsSettingsRepository _settings;

    private readonly OperationsShipmentIntake _intake;

    public GetOperationsShipmentUseCase(
        OperationsOwnerCheck owner,
        OperationsReceivingScope scope,
        IOperationsReceivingRepository receiving,
        IOperationsCustomerRepository customers,
        IOperationsLocationRepository locations,
        IOperationsShippingServiceRepository services,
        IOperationsSettingsRepository settings,
        OperationsShipmentIntake intake)
    {
        _owner = owner ?? throw new ArgumentNullException(nameof(owner));
        _scope = scope ?? throw new ArgumentNullException(nameof(scope));
        _receiving = receiving ?? throw new ArgumentNullException(nameof(receiving));
        _customers = customers ?? throw new ArgumentNullException(nameof(customers));
        _locations = locations ?? throw new ArgumentNullException(nameof(locations));
        _services = services ?? throw new ArgumentNullException(nameof(services));
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        _intake = intake ?? throw new ArgumentNullException(nameof(intake));
    }

    public async Task<GetOperationsShipmentResponse> ExecuteAsync(
        OperationsAuthorizationContext context,
        GetOperationsShipmentRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        if (await _owner.LoadAsync(context, cancellationToken) is not { } access)
        {
            return GetOperationsShipmentResponse.Failure(OperationsAccessErrors.SignInRequired);
        }

        if (await _receiving.GetShipmentAsync(request.ShipmentId, cancellationToken) is not { } shipment
            || await _scope.CoveredAsync(access, OperationsPermissions.ShipmentsView, shipment.OriginWarehouseId, cancellationToken) is not { } origin)
        {
            return GetOperationsShipmentResponse.Failure(OperationsShipmentErrors.NotFound);
        }

        Vixly.Operations.Domain.Customers.OperationsCustomer? customer = await _customers.GetAsync(shipment.CustomerId, cancellationToken);
        bool named = access.Has(OperationsPermissions.CustomersView) || access.IsAdministrator;
        IReadOnlyList<OperationsParcel> parcels = await _receiving.ListShipmentParcelsAsync(shipment.Id, cancellationToken);
        IReadOnlyList<Vixly.Operations.Domain.Receiving.OperationsPreAlert> preAlerts = (await _receiving.ListShipmentPreAlertsAsync(shipment.Id, cancellationToken))
            .Where(preAlert => preAlert.Status == Vixly.Operations.Domain.Receiving.OperationsPreAlertStatus.Open).ToList();
        string? destination = shipment.DestinationWarehouseId is { } destinationId ? (await _locations.GetWarehouseAsync(destinationId, cancellationToken))?.Name
            : shipment.DestinationBranchId is { } branchId ? (await _locations.GetBranchAsync(branchId, cancellationToken))?.Name : null;

        // The chargeable weight: the greater of the actual and the volumetric weight of the shipment's service (owner, A-9).
        decimal? divisor = shipment.ServiceCode is { } code
            ? (await _services.ListAsync(cancellationToken)).FirstOrDefault(item => item.ServiceCode == code)?.VolumetricDivisor
              ?? Vixly.Contracts.Platform.Catalog.PlatformShippingServices.Find(code)?.DefaultVolumetricDivisor
            : null;
        List<OperationsShipmentParcel> rows = parcels.Select(parcel => new OperationsShipmentParcel(parcel, parcel.IsClearedIn(shipment), Chargeable(parcel, divisor))).ToList();

        bool changeable = shipment.Status == OperationsShipmentStatus.Open && !shipment.IsWaiting && !parcels.Any(ShipmentParcels.Departed)
            && access.CoversWarehouse(OperationsPermissions.ShipmentsManage, origin.Id, origin.BranchId);

        return GetOperationsShipmentResponse.Success(shipment, origin, destination, customer?.Code, named ? customer?.FullName : null, preAlerts, rows,
            changeable ? await _intake.ServicesAsync(shipment.CustomerId, cancellationToken) : Array.Empty<string>(), changeable, await _settings.FindAsync(cancellationToken));
    }

    private static decimal? Chargeable(OperationsParcel parcel, decimal? divisor) =>
        parcel.WeightKg is not { } kg ? null
            : divisor is { } by and > 0 && parcel.LengthCm is { } l && parcel.WidthCm is { } w && parcel.HeightCm is { } h ? Math.Max(kg, decimal.Round(l * w * h / by, 3)) : kg;
}
