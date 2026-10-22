using System.Globalization;
using Vixly.Operations.Application.Bundles;
using Vixly.Operations.Application.Ports;
using Vixly.Operations.Application.Security;
using Vixly.Operations.Application.Settings;
using Vixly.Operations.Domain.Access;
using Vixly.Operations.Domain.Bundles;
using Vixly.Operations.Domain.Locations;
using Vixly.Operations.Domain.Settings;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Bundles.GetOperationsRepackBoxLabel;

// =========================================================
// FILE: GetOperationsRepackBoxLabelUseCase.cs
//
// PURPOSE:
// The 4 x 6 label of a repack box (Slice 6A, D-4).
//
// RESPONSIBILITIES:
// - Run the rule and return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - ops.bundles.manage or ops.bundles.fill, at its warehouse
// =========================================================
public sealed class GetOperationsRepackBoxLabelUseCase
{
    private readonly OperationsBundleDesk _desk;

    private readonly IOperationsBundleRepository _bundles;

    private readonly IOperationsReceivingRepository _receiving;

    private readonly IOperationsLocationRepository _locations;

    private readonly IOperationsBrandRepository _brands;

    private readonly IOperationsSettingsRepository _settings;

    private readonly IOperationsLabelRenderer _renderer;

    private readonly TimeProvider _timeProvider;

    public GetOperationsRepackBoxLabelUseCase(
        OperationsBundleDesk desk,
        IOperationsBundleRepository bundles,
        IOperationsReceivingRepository receiving,
        IOperationsLocationRepository locations,
        IOperationsBrandRepository brands,
        IOperationsSettingsRepository settings,
        IOperationsLabelRenderer renderer,
        TimeProvider timeProvider)
    {
        _desk = desk ?? throw new ArgumentNullException(nameof(desk));
        _bundles = bundles ?? throw new ArgumentNullException(nameof(bundles));
        _receiving = receiving ?? throw new ArgumentNullException(nameof(receiving));
        _locations = locations ?? throw new ArgumentNullException(nameof(locations));
        _brands = brands ?? throw new ArgumentNullException(nameof(brands));
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        _renderer = renderer ?? throw new ArgumentNullException(nameof(renderer));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public async Task<GetOperationsRepackBoxLabelResponse> ExecuteAsync(
        OperationsAuthorizationContext context,
        GetOperationsRepackBoxLabelRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);
        (OperationsAccessView? access, IReadOnlyList<OperationsWarehouse> warehouses, Error? refused) = await _desk.OpenAsync(context, new[] { OperationsPermissions.BundlesManage, OperationsPermissions.BundlesFill }, cancellationToken);

        if (refused is not null)
        {
            return GetOperationsRepackBoxLabelResponse.Failure(refused);
        }

        if (await _bundles.GetBoxAsync(request.BoxId, cancellationToken) is not { } box || warehouses.FirstOrDefault(item => item.Id == box.WarehouseId) is not { } warehouse
            || box.Status == OperationsRepackBoxStatus.Voided || await _receiving.GetShipmentAsync(box.ShipmentId, cancellationToken) is not { } shipment)
        {
            return GetOperationsRepackBoxLabelResponse.Failure(OperationsBundleErrors.NotFound);
        }

        Vixly.Operations.Domain.Settings.OperationsTenantSettings settings = await _settings.FindAsync(cancellationToken)
            ?? Vixly.Operations.Domain.Settings.OperationsTenantSettings.CreateDefault(context.RequiredTenantId, _timeProvider.GetUtcNow());
        OperationsWarehouse? destination = shipment.DestinationWarehouseId is { } destinationId ? await _locations.GetWarehouseAsync(destinationId, cancellationToken) : null;
        string? weight = box.WeightKg is { } kg
            ? OperationsUnits.ToShownWeight(kg, settings.WeightUnit).ToString("0.##", CultureInfo.InvariantCulture) + (settings.WeightUnit == OperationsWeightUnit.Pound ? " lb" : " kg")
            : null;
        OperationsRepackBoxLabel label = new((await _brands.FindAsync(cancellationToken))?.DisplayName ?? string.Empty, box.Number, shipment.Number, warehouse.Code,
            destination?.Code ?? shipment.DestinationCountryCode ?? string.Empty, shipment.ServiceCode, shipment.RecipientName ?? string.Empty, box.ParcelCount, weight);
        return GetOperationsRepackBoxLabelResponse.Success(_renderer.RenderRepackBoxLabel(label), box.Number);
    }
}
