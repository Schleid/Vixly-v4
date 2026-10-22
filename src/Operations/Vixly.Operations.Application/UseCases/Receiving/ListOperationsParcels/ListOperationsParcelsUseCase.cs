using Vixly.Operations.Application.Ports;
using Vixly.Operations.Application.Receiving;
using Vixly.Operations.Application.Security;
using Vixly.Operations.Domain.Access;
using Vixly.Operations.Domain.Locations;
using Vixly.Operations.Domain.Receiving;
using Vixly.Operations.Domain.Settings;
using Vixly.Operations.Domain.Tracking;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Receiving.ListOperationsParcels;

// =========================================================
// FILE: ListOperationsParcelsUseCase.cs
//
// PURPOSE:
// Lists the parcels a staff member may see (Slice 3B).
//
// RESPONSIBILITIES:
// - Run the rule and return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - An empty warehouse choice shows every warehouse the person may see
// =========================================================
public sealed class ListOperationsParcelsUseCase
{
    private readonly OperationsOwnerCheck _owner;

    private readonly OperationsReceivingScope _scope;

    private readonly IOperationsReceivingRepository _receiving;

    private readonly OperationsParcelReader _reader;

    private readonly IOperationsSettingsRepository _settings;

    public ListOperationsParcelsUseCase(
        OperationsOwnerCheck owner,
        OperationsReceivingScope scope,
        IOperationsReceivingRepository receiving,
        OperationsParcelReader reader,
        IOperationsSettingsRepository settings)
    {
        _owner = owner ?? throw new ArgumentNullException(nameof(owner));
        _scope = scope ?? throw new ArgumentNullException(nameof(scope));
        _receiving = receiving ?? throw new ArgumentNullException(nameof(receiving));
        _reader = reader ?? throw new ArgumentNullException(nameof(reader));
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
    }

    public async Task<ListOperationsParcelsResponse> ExecuteAsync(
        OperationsAuthorizationContext context,
        ListOperationsParcelsRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        if (await _owner.LoadAsync(context, cancellationToken) is not { } access)
        {
            return ListOperationsParcelsResponse.Failure(OperationsAccessErrors.SignInRequired);
        }

        IReadOnlyList<OperationsWarehouse> warehouses = await _scope.WarehousesAsync(access, OperationsPermissions.ParcelsView, false, cancellationToken);

        if (warehouses.Count == 0)
        {
            return ListOperationsParcelsResponse.Failure(OperationsAccessErrors.PermissionRequired);
        }

        // Parcels are scoped by place: a warehouse grant shows that warehouse's parcels only (design section 8).
        List<Guid> shown = warehouses.Where(warehouse => request.WarehouseId is null || warehouse.Id == request.WarehouseId).Select(warehouse => warehouse.Id).ToList();
        string? search = string.IsNullOrWhiteSpace(request.Search) ? null : request.Search.Trim();

        if (search is { Length: > 60 })
        {
            search = search[..60];
        }

        (IReadOnlyList<OperationsParcel> items, int total) = await _receiving.ListParcelsAsync(new OperationsParcelFilter(shown, search, request.UnidentifiedOnly, request.HeldOnly),
            Math.Max(request.Page, 1), PageSize, cancellationToken);
        OperationsTenantSettings? settings = await _settings.FindAsync(cancellationToken);

        return ListOperationsParcelsResponse.Success(await _reader.SummariesAsync(items, cancellationToken), total, warehouses, settings?.WeightUnit ?? OperationsWeightUnit.Kilogram);
    }

    public const int PageSize = 25;
    
}
