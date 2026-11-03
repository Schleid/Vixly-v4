using Vixly.Operations.Application.Audit;
using Vixly.Operations.Application.Items;
using Vixly.Operations.Application.Ports;
using Vixly.Operations.Application.Receiving;
using Vixly.Operations.Application.Security;
using Vixly.Operations.Domain.Access;
using Vixly.Operations.Domain.Common;
using Vixly.Operations.Domain.Items;
using Vixly.Operations.Domain.Tracking;
using Vixly.Shared.Kernel.Domain;
using Vixly.Shared.Kernel.Errors;
using Vixly.Operations.Application.Bundles;
using Vixly.Operations.Domain.Locations;

namespace Vixly.Operations.Application.UseCases.Items.ListOperationsItemsToReview;

// =========================================================
// FILE: ListOperationsItemsToReviewUseCase.cs
//
// PURPOSE:
// The parcels at origin whose items still need a customs code or a country of origin (owner, 2026-11-01).
//
// RESPONSIBILITIES:
// - Run the rule and return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - Oldest first, at most 200; only the warehouses where the person checks parcels
// =========================================================
public sealed class ListOperationsItemsToReviewUseCase
{
    public const int MaxShown = 200;

    private readonly OperationsBundleDesk _desk;

    private readonly IOperationsItemRepository _items;

    private readonly IOperationsSettingsRepository _settings;

    public ListOperationsItemsToReviewUseCase(OperationsBundleDesk desk, IOperationsItemRepository items, IOperationsSettingsRepository settings)
    {
        _desk = desk ?? throw new ArgumentNullException(nameof(desk));
        _items = items ?? throw new ArgumentNullException(nameof(items));
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
    }

    public async Task<ListOperationsItemsToReviewResponse> ExecuteAsync(
        OperationsAuthorizationContext context,
        ListOperationsItemsToReviewRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        (_, IReadOnlyList<OperationsWarehouse> warehouses, Error? refused) = await _desk.OpenAsync(context, new[] { OperationsPermissions.InspectionCheck }, cancellationToken);

        if (refused is not null)
        {
            return ListOperationsItemsToReviewResponse.Failure(refused);
        }

        Vixly.Operations.Domain.Settings.OperationsTenantSettings? settings = await _settings.FindAsync(cancellationToken);
        IReadOnlyList<OperationsParcel> parcels = await _items.ListParcelsToReviewAsync(warehouses.Select(warehouse => warehouse.Id).ToList(), settings?.ItemsRequiredSinceUtc,
            MaxShown, cancellationToken);
        IReadOnlyList<OperationsParcelItem> items = await _items.ListForParcelsAsync(parcels, cancellationToken);
        Dictionary<Guid, string> codes = warehouses.ToDictionary(warehouse => warehouse.Id, warehouse => warehouse.Code);

        List<OperationsItemReviewRow> rows = parcels.Select(parcel =>
        {
            List<OperationsParcelItem> own = items.Where(item => item.ParcelId == parcel.Id).ToList();

            if (own.Count == 0 && parcel.PreAlertId is { } preAlert)
            {
                own = items.Where(item => item.ParcelId == null && item.PreAlertId == preAlert).ToList();
            }

            return new OperationsItemReviewRow(parcel.Id, parcel.Number ?? string.Empty, codes.GetValueOrDefault(parcel.WarehouseId ?? Guid.Empty) ?? string.Empty, parcel.CreatedUtc,
                parcel.DeclaredContents, own.Count, own.Count == 0 ? 1 : own.Count(item => !item.IsComplete));
        }).ToList();

        return ListOperationsItemsToReviewResponse.Success(rows, settings?.ItemsRequiredSinceUtc is not null);
    }
}
