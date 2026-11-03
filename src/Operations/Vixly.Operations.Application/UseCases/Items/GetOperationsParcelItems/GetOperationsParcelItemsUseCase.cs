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

namespace Vixly.Operations.Application.UseCases.Items.GetOperationsParcelItems;

// =========================================================
// FILE: GetOperationsParcelItemsUseCase.cs
//
// PURPOSE:
// The items of a parcel, for staff to classify for customs (owner, 2026-11-01).
//
// RESPONSIBILITIES:
// - Run the rule and return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - No parcel or no right to see it: the same not-found answer (S-3)
// =========================================================
public sealed class GetOperationsParcelItemsUseCase
{
    private readonly OperationsOwnerCheck _owner;

    private readonly OperationsReceivingScope _scope;

    private readonly IOperationsTrackingRepository _tracking;

    private readonly IOperationsItemRepository _items;

    private readonly OperationsItemCatalogue _catalogue;

    private readonly IOperationsUnitOfWork _unitOfWork;

    private readonly TimeProvider _timeProvider;

    public GetOperationsParcelItemsUseCase(OperationsOwnerCheck owner, OperationsReceivingScope scope, IOperationsTrackingRepository tracking, IOperationsItemRepository items, OperationsItemCatalogue catalogue, IOperationsUnitOfWork unitOfWork, TimeProvider timeProvider)
    {
        _owner = owner ?? throw new ArgumentNullException(nameof(owner));
        _scope = scope ?? throw new ArgumentNullException(nameof(scope));
        _tracking = tracking ?? throw new ArgumentNullException(nameof(tracking));
        _items = items ?? throw new ArgumentNullException(nameof(items));
        _catalogue = catalogue ?? throw new ArgumentNullException(nameof(catalogue));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public async Task<GetOperationsParcelItemsResponse> ExecuteAsync(
        OperationsAuthorizationContext context,
        GetOperationsParcelItemsRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        if (await _owner.LoadAsync(context, cancellationToken) is not { } access)
        {
            return GetOperationsParcelItemsResponse.Failure(OperationsAccessErrors.SignInRequired);
        }

        // Another warehouse's parcel and a missing parcel give the same answer (S-3).
        if (await _tracking.GetParcelAsync(request.ParcelId, cancellationToken) is not { WarehouseId: { } warehouseId } parcel
            || await _scope.CoveredAsync(access, OperationsPermissions.ParcelsView, warehouseId, cancellationToken) is not { } warehouse)
        {
            return GetOperationsParcelItemsResponse.Failure(Vixly.Operations.Domain.Receiving.OperationsReceivingErrors.ParcelNotFound);
        }

        bool canEdit = parcel.IsAtOrigin && parcel.IsIdentified && access.CoversWarehouse(OperationsPermissions.InspectionCheck, warehouse.Id, warehouse.BranchId);
        List<OperationsItemRow> rows = (await _items.ListForParcelAsync(parcel.Id, parcel.PreAlertId, cancellationToken)).Select(OperationsItemRow.Of).ToList();

        // Declared before items existed: the single declaration, shown as one item to classify.
        if (rows.Count == 0 && parcel.DeclaredContents is { } contents)
        {
            rows.Add(new OperationsItemRow(null, 1, contents, 1m, parcel.DeclaredValue ?? 0m, parcel.CurrencyCode ?? string.Empty, false, null, null, null, OperationsItemUnit.Pieces, false));
        }

        (IReadOnlyList<OperationsItemCatalogueEntry> catalogue, bool loaded) = canEdit
            ? await _catalogue.ListAsync(context.RequiredTenantId, true, _timeProvider.GetUtcNow(), cancellationToken)
            : (Array.Empty<OperationsItemCatalogueEntry>(), false);

        // The starter list went in just now (once per company).
        if (loaded && !await _unitOfWork.SaveChangesAsync(cancellationToken))
        {
            _unitOfWork.DiscardChanges();
        }

        return GetOperationsParcelItemsResponse.Success(rows, catalogue, canEdit);
    }
}
