using Vixly.Operations.Application.Audit;
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

namespace Vixly.Operations.Application.UseCases.Supplies.SetOperationsSupplyReorderLevel;

// =========================================================
// FILE: SetOperationsSupplyReorderLevelUseCase.cs
//
// PURPOSE:
// The stock level at which a supply shows as low at a warehouse (Slice 5E, S-8).
//
// RESPONSIBILITIES:
// - Run the rule and return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - ops.supplies.stock, for the warehouse concerned (Slice 5E, S-10)
// - Refused while the company does not count its supplies stock (S-9)
// =========================================================
public sealed class SetOperationsSupplyReorderLevelUseCase
{
    private readonly OperationsSupplyDesk _desk;

    private readonly IOperationsSupplyRepository _supplies;

    private readonly OperationsAuditTrail _audit;

    private readonly IOperationsUnitOfWork _unitOfWork;

    private readonly TimeProvider _timeProvider;

    public SetOperationsSupplyReorderLevelUseCase(
        OperationsSupplyDesk desk,
        IOperationsSupplyRepository supplies,
        OperationsAuditTrail audit,
        IOperationsUnitOfWork unitOfWork,
        TimeProvider timeProvider)
    {
        _desk = desk ?? throw new ArgumentNullException(nameof(desk));
        _supplies = supplies ?? throw new ArgumentNullException(nameof(supplies));
        _audit = audit ?? throw new ArgumentNullException(nameof(audit));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public async Task<SetOperationsSupplyReorderLevelResponse> ExecuteAsync(
        OperationsAuthorizationContext context,
        SetOperationsSupplyReorderLevelRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        (OperationsAccessView? access, IReadOnlyList<OperationsWarehouse> warehouses, OperationsTenantSettings settings, Error? refused) = await _desk.OpenAsync(context, cancellationToken);

        if (refused is not null)
        {
            return SetOperationsSupplyReorderLevelResponse.Failure(refused);
        }

        if (!settings.CountSupplyStock)
        {
            return SetOperationsSupplyReorderLevelResponse.Failure(OperationsSupplyStockErrors.NotCounting);
        }

        if (warehouses.FirstOrDefault(item => item.Id == request.WarehouseId) is not { } warehouse)
        {
            return SetOperationsSupplyReorderLevelResponse.Failure(OperationsAccessErrors.PermissionRequired);
        }

        if (await _supplies.GetAsync(request.SupplyId, cancellationToken) is not { } supply)
        {
            return SetOperationsSupplyReorderLevelResponse.Failure(OperationsSupplyErrors.NotFound);
        }

        DateTimeOffset utcNow = _timeProvider.GetUtcNow();
        OperationsSupplyStock? stock = await _supplies.GetStockAsync(supply.Id, warehouse.Id, cancellationToken);

        if (stock is null)
        {
            stock = OperationsSupplyStock.Create(context.RequiredTenantId, supply.Id, warehouse.Id, utcNow);
            _supplies.Add(stock);
        }

        try
        {
            stock.Reorder(request.Level, utcNow);
        }
        catch (DomainException exception)
        {
            return SetOperationsSupplyReorderLevelResponse.Failure(exception.Error);
        }

        _audit.Record(access!.User.Id, "ops.supplies.reorder_level_set", nameof(OperationsSupply), supply.Id, after: new { warehouse = warehouse.Code, level = request.Level });

        return await _unitOfWork.SaveChangesAsync(cancellationToken)
            ? SetOperationsSupplyReorderLevelResponse.Success()
            : SetOperationsSupplyReorderLevelResponse.Failure(OperationsCommonErrors.ConcurrencyConflict);
    }
}
