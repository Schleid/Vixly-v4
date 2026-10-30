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

namespace Vixly.Operations.Application.UseCases.Supplies.AdjustOperationsSupplyStock;

// =========================================================
// FILE: AdjustOperationsSupplyStockUseCase.cs
//
// PURPOSE:
// Supplies stock adjusted by hand at a warehouse (Slice 5E, S-4): more or fewer, with a reason and a note.
//
// RESPONSIBILITIES:
// - Run the rule and return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - ops.supplies.stock, for the warehouse concerned (Slice 5E, S-10)
// - Refused while the company does not count its supplies stock (S-9)
// =========================================================
public sealed class AdjustOperationsSupplyStockUseCase
{
    private readonly OperationsSupplyDesk _desk;

    private readonly IOperationsSupplyRepository _supplies;

    private readonly OperationsSupplyStockKeeper _keeper;

    private readonly OperationsAuditTrail _audit;

    private readonly IOperationsUnitOfWork _unitOfWork;

    private readonly TimeProvider _timeProvider;

    public AdjustOperationsSupplyStockUseCase(
        OperationsSupplyDesk desk,
        IOperationsSupplyRepository supplies,
        OperationsSupplyStockKeeper keeper,
        OperationsAuditTrail audit,
        IOperationsUnitOfWork unitOfWork,
        TimeProvider timeProvider)
    {
        _desk = desk ?? throw new ArgumentNullException(nameof(desk));
        _supplies = supplies ?? throw new ArgumentNullException(nameof(supplies));
        _keeper = keeper ?? throw new ArgumentNullException(nameof(keeper));
        _audit = audit ?? throw new ArgumentNullException(nameof(audit));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public async Task<AdjustOperationsSupplyStockResponse> ExecuteAsync(
        OperationsAuthorizationContext context,
        AdjustOperationsSupplyStockRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        (OperationsAccessView? access, IReadOnlyList<OperationsWarehouse> warehouses, OperationsTenantSettings settings, Error? refused) = await _desk.OpenAsync(context, cancellationToken);

        if (refused is not null)
        {
            return AdjustOperationsSupplyStockResponse.Failure(refused);
        }

        if (!settings.CountSupplyStock)
        {
            return AdjustOperationsSupplyStockResponse.Failure(OperationsSupplyStockErrors.NotCounting);
        }

        if (warehouses.FirstOrDefault(item => item.Id == request.WarehouseId) is not { } warehouse)
        {
            return AdjustOperationsSupplyStockResponse.Failure(OperationsAccessErrors.PermissionRequired);
        }

        if (request.Key == Guid.Empty)
        {
            return AdjustOperationsSupplyStockResponse.Failure(OperationsSupplyStockErrors.LinesInvalid);
        }

        if (await _supplies.LinkUsedAsync(request.Key, cancellationToken))
        {
            return AdjustOperationsSupplyStockResponse.Success();
        }

        if (await _supplies.GetAsync(request.SupplyId, cancellationToken) is not { } supply)
        {
            return AdjustOperationsSupplyStockResponse.Failure(OperationsSupplyErrors.NotFound);
        }

        DateTimeOffset utcNow = _timeProvider.GetUtcNow();
        OperationsSupplyStock stock;

        try
        {
            stock = await _keeper.MoveAsync(OperationsSupplyMovement.Adjusted(context.RequiredTenantId, supply.Id, warehouse.Id, request.Quantity, request.Reason, request.Note,
                request.Key, access!.User.Id, utcNow), utcNow, cancellationToken);
        }
        catch (DomainException exception)
        {
            return AdjustOperationsSupplyStockResponse.Failure(exception.Error);
        }

        _audit.Record(access.User.Id, "ops.supplies.stock_adjusted", nameof(OperationsSupply), supply.Id,
            after: new { warehouse = warehouse.Code, quantity = request.Quantity, reason = request.Reason.ToString(), note = request.Note, onHand = stock.OnHand });

        return await _unitOfWork.SaveChangesAsync(cancellationToken)
            ? AdjustOperationsSupplyStockResponse.Success()
            : AdjustOperationsSupplyStockResponse.Failure(OperationsCommonErrors.ConcurrencyConflict);
    }
}
