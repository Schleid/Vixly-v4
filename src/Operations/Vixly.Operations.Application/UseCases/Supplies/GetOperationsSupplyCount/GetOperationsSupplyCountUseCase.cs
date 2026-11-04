using Vixly.Operations.Application.Ports;
using Vixly.Operations.Application.Security;
using Vixly.Operations.Application.Supplies;
using Vixly.Operations.Domain.Access;
using Vixly.Operations.Domain.Common;
using Vixly.Operations.Domain.Locations;
using Vixly.Operations.Domain.Settings;
using Vixly.Operations.Domain.Supplies;
using Vixly.Shared.Kernel.Domain;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Supplies.GetOperationsSupplyCount;

// =========================================================
// FILE: GetOperationsSupplyCountUseCase.cs
//
// PURPOSE:
// A stock count with the supplies to count and their stock on hand (Slice 5E, S-7).
//
// RESPONSIBILITIES:
// - Run the rule and return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - ops.supplies.stock, for the count's warehouse
// =========================================================
public sealed class GetOperationsSupplyCountUseCase
{
    private readonly OperationsSupplyDesk _desk;

    private readonly IOperationsSupplyRepository _supplies;

    public GetOperationsSupplyCountUseCase(
        OperationsSupplyDesk desk,
        IOperationsSupplyRepository supplies)
    {
        _desk = desk ?? throw new ArgumentNullException(nameof(desk));
        _supplies = supplies ?? throw new ArgumentNullException(nameof(supplies));
    }

    public async Task<GetOperationsSupplyCountResponse> ExecuteAsync(
        OperationsAuthorizationContext context,
        GetOperationsSupplyCountRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        (OperationsAccessView? _, IReadOnlyList<OperationsWarehouse> warehouses, OperationsTenantSettings settings, Error? refused) = await _desk.OpenAsync(context, cancellationToken);

        if (refused is not null)
        {
            return GetOperationsSupplyCountResponse.Failure(refused);
        }

        if (await _supplies.GetCountAsync(request.CountId, cancellationToken) is not { } count
            || warehouses.FirstOrDefault(item => item.Id == count.WarehouseId) is not { } warehouse)
        {
            return GetOperationsSupplyCountResponse.Failure(OperationsSupplyStockErrors.CountNotFound);
        }

        return GetOperationsSupplyCountResponse.Success(count, warehouse, await _supplies.ListAsync(cancellationToken),
            await _supplies.ListStockAsync(warehouse.Id, cancellationToken));
    }
}
