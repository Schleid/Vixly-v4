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

namespace Vixly.Operations.Application.UseCases.Items.SaveOperationsParcelItems;

// =========================================================
// FILE: SaveOperationsParcelItemsUseCase.cs
//
// PURPOSE:
// Staff classify a parcel's items for customs: description on documents, customs code, country of origin, unit (owner, 2026-11-01).
//
// RESPONSIBILITIES:
// - Run the rule and return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - D-C6: the customer's declaration (words, quantity, value) is never changed here, only classified
// =========================================================
public sealed class SaveOperationsParcelItemsUseCase
{
    private readonly OperationsOwnerCheck _owner;

    private readonly OperationsReceivingScope _scope;

    private readonly IOperationsTrackingRepository _tracking;

    private readonly IOperationsItemRepository _items;

    private readonly OperationsItemCatalogue _catalogue;

    private readonly OperationsAuditTrail _audit;

    private readonly IOperationsUnitOfWork _unitOfWork;

    private readonly TimeProvider _timeProvider;

    public SaveOperationsParcelItemsUseCase(OperationsOwnerCheck owner, OperationsReceivingScope scope, IOperationsTrackingRepository tracking, IOperationsItemRepository items, OperationsItemCatalogue catalogue, OperationsAuditTrail audit, IOperationsUnitOfWork unitOfWork, TimeProvider timeProvider)
    {
        _owner = owner ?? throw new ArgumentNullException(nameof(owner));
        _scope = scope ?? throw new ArgumentNullException(nameof(scope));
        _tracking = tracking ?? throw new ArgumentNullException(nameof(tracking));
        _items = items ?? throw new ArgumentNullException(nameof(items));
        _catalogue = catalogue ?? throw new ArgumentNullException(nameof(catalogue));
        _audit = audit ?? throw new ArgumentNullException(nameof(audit));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public async Task<SaveOperationsParcelItemsResponse> ExecuteAsync(
        OperationsAuthorizationContext context,
        SaveOperationsParcelItemsRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        if (await _owner.LoadAsync(context, cancellationToken) is not { } access)
        {
            return SaveOperationsParcelItemsResponse.Failure(OperationsAccessErrors.SignInRequired);
        }

        if (await _tracking.GetParcelAsync(request.ParcelId, cancellationToken) is not { WarehouseId: { } warehouseId } parcel
            || await _scope.CoveredAsync(access, OperationsPermissions.ParcelsView, warehouseId, cancellationToken) is not { } warehouse)
        {
            return SaveOperationsParcelItemsResponse.Failure(Vixly.Operations.Domain.Receiving.OperationsReceivingErrors.ParcelNotFound);
        }

        if (!parcel.IsAtOrigin || !parcel.IsIdentified || !access.CoversWarehouse(OperationsPermissions.InspectionCheck, warehouse.Id, warehouse.BranchId))
        {
            return SaveOperationsParcelItemsResponse.Failure(OperationsAccessErrors.PermissionRequired);
        }

        DateTimeOffset utcNow = _timeProvider.GetUtcNow();
        List<OperationsParcelItem> items = (await _items.ListForParcelAsync(parcel.Id, parcel.PreAlertId, cancellationToken)).ToList();
        (IReadOnlyList<OperationsItemCatalogueEntry> catalogue, _) = await _catalogue.ListAsync(context.RequiredTenantId, true, utcNow, cancellationToken);
        List<OperationsItemCatalogueEntry> known = catalogue.ToList();

        try
        {
            // The single declaration made before items existed becomes the parcel's first item (the customer's words, unchanged).
            if (items.Count == 0)
            {
                if (parcel.DeclaredContents is not { } contents)
                {
                    return SaveOperationsParcelItemsResponse.Failure(OperationsItemErrors.NotFound);
                }

                string words = contents.Length > OperationsParcelItem.WordsMaxLength ? contents[..OperationsParcelItem.WordsMaxLength] : contents;
                OperationsParcelItem first = OperationsParcelItem.Declare(context.RequiredTenantId, words, 1m, parcel.DeclaredValue ?? 0m,
                    parcel.CurrencyCode ?? Vixly.Operations.Domain.Settings.OperationsTenantSettings.DefaultCurrency, false, 1, utcNow);
                first.ForParcel(parcel.Id, utcNow);
                _items.Add(first);
                items.Add(first);
            }

            // A pre-alert's items become the parcel's own once staff work on them.
            foreach (OperationsParcelItem item in items.Where(item => item.ParcelId is null))
            {
                item.ForParcel(parcel.Id, utcNow);
            }

            foreach (OperationsItemClassification line in request.Items)
            {
                OperationsParcelItem? item = line.ItemId is { } itemId ? items.FirstOrDefault(found => found.Id == itemId) : items.Count == 1 ? items[0] : null;

                if (item is null)
                {
                    return SaveOperationsParcelItemsResponse.Failure(OperationsItemErrors.NotFound);
                }

                string? description = string.IsNullOrWhiteSpace(line.Description) ? null : line.Description.Trim();
                OperationsItemCatalogueEntry? entry = description is null ? null
                    : known.FirstOrDefault(found => string.Equals(found.Description, description, StringComparison.OrdinalIgnoreCase));
                string? code = string.IsNullOrWhiteSpace(line.HsCode) ? entry?.HsCode : line.HsCode;
                OperationsItemUnit unit = entry is not null && string.IsNullOrWhiteSpace(line.HsCode) ? entry.Unit : line.Unit;
                bool changedEntry = entry is not null && entry.Id != item.CatalogueEntryId;
                item.Classify(entry?.Description ?? description, code, line.OriginCountryCode, unit, entry?.Id, access.User.Id, utcNow);

                if (changedEntry)
                {
                    entry!.Used(utcNow);
                }

                // A new everyday item: kept in the catalogue for next time.
                if (line.AddToCatalogue && entry is null && description is not null && item.HsCode is not null)
                {
                    if (known.Count >= OperationsItemCatalogueEntry.MaxPerCompany)
                    {
                        return SaveOperationsParcelItemsResponse.Failure(OperationsItemErrors.CatalogueFull);
                    }

                    OperationsItemCatalogueEntry added = OperationsItemCatalogueEntry.Create(context.RequiredTenantId, description, item.Words, item.HsCode, item.Unit, utcNow);
                    _items.AddCatalogueEntry(added);
                    known.Add(added);
                }
            }
        }
        catch (DomainException exception)
        {
            return SaveOperationsParcelItemsResponse.Failure(exception.Error);
        }

        _audit.Record(access.User.Id, "ops.items.classified", nameof(OperationsParcel), parcel.Id,
            after: items.Select(item => new { item.Line, code = item.HsCode, origin = item.OriginCountryCode }).ToArray());

        return await _unitOfWork.SaveChangesAsync(cancellationToken)
            ? SaveOperationsParcelItemsResponse.Success(items.Count(item => !item.IsComplete))
            : SaveOperationsParcelItemsResponse.Failure(OperationsCommonErrors.ConcurrencyConflict);
    }
}
