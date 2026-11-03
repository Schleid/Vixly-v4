using Vixly.Operations.Application.Ports;
using Vixly.Operations.Application.Receiving;
using Vixly.Operations.Application.Security;
using Vixly.Operations.Domain.Access;
using Vixly.Operations.Domain.Locations;
using Vixly.Operations.Domain.Receiving;
using Vixly.Operations.Domain.Settings;
using Vixly.Operations.Domain.Tracking;

namespace Vixly.Operations.Application.UseCases.Receiving.ListOperationsOnHandParcels;

// =========================================================
// FILE: ListOperationsOnHandParcelsUseCase.cs
//
// PURPOSE:
// Lists the parcels physically on hand in the viewer's warehouses, oldest first, with their totals (owner, 2026-10-22: parcel inventory).
//
// RESPONSIBILITIES:
// - Scope to the warehouses where the viewer may see parcels
// - Filter by warehouse, shelf or zone, without a shelf, on hold, days on hand, a search
// - Total the pieces and the weight over all pages
//
// IMPORTANT NOTES:
// - On hand: at its origin warehouse and not yet departed; the destination side comes with full inventory
// - Paged (P-1): 50 a page; two queries (page and totals) and the summaries' lookups
// =========================================================
public sealed class ListOperationsOnHandParcelsUseCase
{
    private const int PageSize = 50;

    private const int MaxDays = 3650;

    private readonly OperationsOwnerCheck _owner;

    private readonly OperationsReceivingScope _scope;

    private readonly IOperationsReceivingRepository _receiving;

    private readonly OperationsParcelReader _reader;

    private readonly IOperationsSettingsRepository _settings;

    private readonly TimeProvider _timeProvider;

    public ListOperationsOnHandParcelsUseCase(
        OperationsOwnerCheck owner,
        OperationsReceivingScope scope,
        IOperationsReceivingRepository receiving,
        OperationsParcelReader reader,
        IOperationsSettingsRepository settings,
        TimeProvider timeProvider)
    {
        _owner = owner ?? throw new ArgumentNullException(nameof(owner));
        _scope = scope ?? throw new ArgumentNullException(nameof(scope));
        _receiving = receiving ?? throw new ArgumentNullException(nameof(receiving));
        _reader = reader ?? throw new ArgumentNullException(nameof(reader));
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public async Task<ListOperationsOnHandParcelsResponse> ExecuteAsync(
        OperationsAuthorizationContext context,
        ListOperationsOnHandParcelsRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        // =====================================================
        // TRUSTED CONTEXT VALIDATION
        // =====================================================
        if (await _owner.LoadAsync(context, cancellationToken) is not { } access)
        {
            return ListOperationsOnHandParcelsResponse.Failure(OperationsAccessErrors.SignInRequired);
        }

        IReadOnlyList<OperationsWarehouse> warehouses = await _scope.WarehousesAsync(access, OperationsPermissions.ParcelsView, false, cancellationToken);

        if (warehouses.Count == 0)
        {
            return ListOperationsOnHandParcelsResponse.Failure(OperationsAccessErrors.PermissionRequired);
        }

        // =====================================================
        // REQUEST VALIDATION
        // =====================================================
        List<Guid> shown = warehouses.Where(warehouse => request.WarehouseId is null || warehouse.Id == request.WarehouseId).Select(warehouse => warehouse.Id).ToList();
        string? search = string.IsNullOrWhiteSpace(request.Search) ? null : request.Search.Trim() is { Length: > 60 } longer ? longer[..60] : request.Search.Trim();
        DateTimeOffset utcNow = _timeProvider.GetUtcNow();
        DateTimeOffset? receivedBefore = request.MinDays is int days and > 0 ? utcNow.AddDays(-Math.Min(days, MaxDays)) : null;
        Guid? place = shown.Count == 1 ? shown[0] : null;

        // =====================================================
        // ON HAND
        // =====================================================
        IReadOnlyList<OperationsStorageLocation> locations = place is { } single ? await _receiving.ListLocationsAsync(single, cancellationToken) : Array.Empty<OperationsStorageLocation>();
        Guid? location = request.LocationId is { } asked && locations.Any(item => item.Id == asked) ? asked : null;
        OperationsParcelFilter filter = new(shown, search, false, request.HeldOnly, true, location, request.Zone, request.UnshelvedOnly, receivedBefore);
        (IReadOnlyList<OperationsParcel> items, int total) = await _receiving.ListParcelsAsync(filter, Math.Max(request.Page, 1), PageSize, cancellationToken);
        (int _, decimal weightKg) = await _receiving.ParcelTotalsAsync(filter, cancellationToken);
        OperationsTenantSettings? settings = await _settings.FindAsync(cancellationToken);

        return ListOperationsOnHandParcelsResponse.Success(await _reader.SummariesAsync(items, cancellationToken), total, weightKg, warehouses, locations,
            settings?.WeightUnit ?? OperationsWeightUnit.Kilogram, utcNow);
    }
}
