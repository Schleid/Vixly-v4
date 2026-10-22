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

namespace Vixly.Operations.Application.UseCases.Supplies.ReceiveOperationsSupplies;

// =========================================================
// FILE: ReceiveOperationsSuppliesUseCase.cs
//
// PURPOSE:
// Supplies received at a warehouse (Slice 5E, S-5): each line adds stock with its cost; the supplier and the reference are kept.
//
// RESPONSIBILITIES:
// - Run the rule and return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - ops.supplies.stock, for the warehouse concerned (Slice 5E, S-10)
// - Refused while the company does not count its supplies stock (S-9)
// =========================================================
public sealed class ReceiveOperationsSuppliesUseCase
{
    private readonly OperationsSupplyDesk _desk;

    private readonly IOperationsSupplyRepository _supplies;

    private readonly OperationsSupplyStockKeeper _keeper;

    private readonly OperationsAuditTrail _audit;

    private readonly IOperationsUnitOfWork _unitOfWork;

    private readonly TimeProvider _timeProvider;

    public ReceiveOperationsSuppliesUseCase(
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

    public async Task<ReceiveOperationsSuppliesResponse> ExecuteAsync(
        OperationsAuthorizationContext context,
        ReceiveOperationsSuppliesRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        (OperationsAccessView? access, IReadOnlyList<OperationsWarehouse> warehouses, OperationsTenantSettings settings, Error? refused) = await _desk.OpenAsync(context, cancellationToken);

        if (refused is not null)
        {
            return ReceiveOperationsSuppliesResponse.Failure(refused);
        }

        if (!settings.CountSupplyStock)
        {
            return ReceiveOperationsSuppliesResponse.Failure(OperationsSupplyStockErrors.NotCounting);
        }

        if (warehouses.FirstOrDefault(item => item.Id == request.WarehouseId) is not { } warehouse)
        {
            return ReceiveOperationsSuppliesResponse.Failure(OperationsAccessErrors.PermissionRequired);
        }

        if (request.Key == Guid.Empty || request.Lines.Count is 0 or > OperationsSupplyStockLimits.MaxLines)
        {
            return ReceiveOperationsSuppliesResponse.Failure(OperationsSupplyStockErrors.LinesInvalid);
        }

        if (await _supplies.LinkUsedAsync(request.Key, cancellationToken))
        {
            return ReceiveOperationsSuppliesResponse.Success();
        }

        HashSet<Guid> known = (await _supplies.ListAsync(cancellationToken)).Select(supply => supply.Id).ToHashSet();
        DateTimeOffset utcNow = _timeProvider.GetUtcNow();

        try
        {
            foreach (OperationsSupplyReceiptLine line in request.Lines)
            {
                if (!known.Contains(line.SupplyId))
                {
                    return ReceiveOperationsSuppliesResponse.Failure(OperationsSupplyErrors.NotFound);
                }

                await _keeper.MoveAsync(OperationsSupplyMovement.Received(context.RequiredTenantId, line.SupplyId, warehouse.Id, line.Quantity, line.UnitCost,
                    request.Supplier, request.Reference, request.Key, access!.User.Id, utcNow), utcNow, cancellationToken);
            }
        }
        catch (DomainException exception)
        {
            return ReceiveOperationsSuppliesResponse.Failure(exception.Error);
        }

        _audit.Record(access!.User.Id, "ops.supplies.stock_received", nameof(OperationsWarehouse), warehouse.Id,
            after: new { lines = request.Lines.Count, units = request.Lines.Sum(line => line.Quantity), supplier = request.Supplier, reference = request.Reference });

        return await _unitOfWork.SaveChangesAsync(cancellationToken)
            ? ReceiveOperationsSuppliesResponse.Success()
            : ReceiveOperationsSuppliesResponse.Failure(OperationsCommonErrors.ConcurrencyConflict);
    }
}
