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

namespace Vixly.Operations.Application.UseCases.Bundles.GetOperationsBundleLabel;

// =========================================================
// FILE: GetOperationsBundleLabelUseCase.cs
//
// PURPOSE:
// The 4 x 6 label of a bundle (Slice 6A, D-2; owner, A-6).
//
// RESPONSIBILITIES:
// - Run the rule and return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - ops.bundles.manage or ops.bundles.fill, at its warehouse
// - Printed again with its weight once closed (D-7)
// =========================================================
public sealed class GetOperationsBundleLabelUseCase
{
    private readonly OperationsBundleDesk _desk;

    private readonly IOperationsBundleRepository _bundles;

    private readonly IOperationsLocationRepository _locations;

    private readonly IOperationsBrandRepository _brands;

    private readonly IOperationsSettingsRepository _settings;

    private readonly IOperationsLabelRenderer _renderer;

    private readonly TimeProvider _timeProvider;

    public GetOperationsBundleLabelUseCase(
        OperationsBundleDesk desk,
        IOperationsBundleRepository bundles,
        IOperationsLocationRepository locations,
        IOperationsBrandRepository brands,
        IOperationsSettingsRepository settings,
        IOperationsLabelRenderer renderer,
        TimeProvider timeProvider)
    {
        _desk = desk ?? throw new ArgumentNullException(nameof(desk));
        _bundles = bundles ?? throw new ArgumentNullException(nameof(bundles));
        _locations = locations ?? throw new ArgumentNullException(nameof(locations));
        _brands = brands ?? throw new ArgumentNullException(nameof(brands));
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        _renderer = renderer ?? throw new ArgumentNullException(nameof(renderer));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public async Task<GetOperationsBundleLabelResponse> ExecuteAsync(
        OperationsAuthorizationContext context,
        GetOperationsBundleLabelRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);
        (OperationsAccessView? access, IReadOnlyList<OperationsWarehouse> warehouses, Error? refused) = await _desk.OpenAsync(context, new[] { OperationsPermissions.BundlesManage, OperationsPermissions.BundlesFill }, cancellationToken);

        if (refused is not null)
        {
            return GetOperationsBundleLabelResponse.Failure(refused);
        }

        if (await _bundles.GetBundleAsync(request.BundleId, cancellationToken) is not { } bundle || warehouses.FirstOrDefault(item => item.Id == bundle.WarehouseId) is not { } warehouse
            || bundle.Status == OperationsBundleStatus.Voided)
        {
            return GetOperationsBundleLabelResponse.Failure(OperationsBundleErrors.NotFound);
        }

        Vixly.Operations.Domain.Settings.OperationsTenantSettings settings = await _settings.FindAsync(cancellationToken)
            ?? Vixly.Operations.Domain.Settings.OperationsTenantSettings.CreateDefault(context.RequiredTenantId, _timeProvider.GetUtcNow());
        OperationsWarehouse? destination = bundle.DestinationWarehouseId is { } destinationId ? await _locations.GetWarehouseAsync(destinationId, cancellationToken) : null;
        string? weight = bundle.WeightKg is { } kg
            ? OperationsUnits.ToShownWeight(kg, settings.WeightUnit).ToString("0.##", CultureInfo.InvariantCulture) + (settings.WeightUnit == OperationsWeightUnit.Pound ? " lb" : " kg")
            : null;
        string? size = bundle.LengthCm is { } length && bundle.WidthCm is { } width && bundle.HeightCm is { } height
            ? string.Join(" x ", new[] { length, width, height }.Select(side => OperationsUnits.ToShownLength(side, settings.DimensionUnit).ToString("0.#", CultureInfo.InvariantCulture)))
                + (settings.DimensionUnit == OperationsDimensionUnit.Inch ? " in" : " cm")
            : null;

        OperationsBundleLabel label = new((await _brands.FindAsync(cancellationToken))?.DisplayName ?? string.Empty, bundle.Number, warehouse.Code,
            destination?.Code ?? bundle.DestinationCountryCode, bundle.ServiceCode, bundle.TypeName, bundle.ParcelCount, weight, size, bundle.Status == OperationsBundleStatus.Closed);
        return GetOperationsBundleLabelResponse.Success(_renderer.RenderBundleLabel(label), bundle.Number);
    }
}
