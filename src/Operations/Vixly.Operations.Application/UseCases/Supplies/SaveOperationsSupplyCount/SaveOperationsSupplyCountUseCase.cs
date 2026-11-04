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

namespace Vixly.Operations.Application.UseCases.Supplies.SaveOperationsSupplyCount;

// =========================================================
// FILE: SaveOperationsSupplyCountUseCase.cs
//
// PURPOSE:
// A stock count saved as the clerk goes, closed (each difference becomes a count correction) or cancelled (Slice 5E, S-7).
//
// RESPONSIBILITIES:
// - Run the rule and return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - ops.supplies.stock, for the warehouse concerned (Slice 5E, S-10)
// - Refused while the company does not count its supplies stock (S-9)
// - A supply not counted is left as it is
// =========================================================
public sealed class SaveOperationsSupplyCountUseCase
{
    private readonly OperationsSupplyDesk _desk;

    private readonly IOperationsSupplyRepository _supplies;

    private readonly OperationsSupplyStockKeeper _keeper;

    private readonly OperationsAuditTrail _audit;

    private readonly IOperationsUnitOfWork _unitOfWork;

    private readonly TimeProvider _timeProvider;

    public SaveOperationsSupplyCountUseCase(
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

    public async Task<SaveOperationsSupplyCountResponse> ExecuteAsync(
        OperationsAuthorizationContext context,
        SaveOperationsSupplyCountRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        (OperationsAccessView? access, IReadOnlyList<OperationsWarehouse> warehouses, OperationsTenantSettings settings, Error? refused) = await _desk.OpenAsync(context, cancellationToken);

        if (refused is not null)
        {
            return SaveOperationsSupplyCountResponse.Failure(refused);
        }

        if (!settings.CountSupplyStock)
        {
            return SaveOperationsSupplyCountResponse.Failure(OperationsSupplyStockErrors.NotCounting);
        }

        if (await _supplies.GetCountAsync(request.CountId, cancellationToken) is not { } count
            || warehouses.FirstOrDefault(item => item.Id == count.WarehouseId) is not { } warehouse)
        {
            return SaveOperationsSupplyCountResponse.Failure(OperationsSupplyStockErrors.CountNotFound);
        }

        DateTimeOffset utcNow = _timeProvider.GetUtcNow();
        int corrections = 0;

        try
        {
            if (!request.Cancel)
            {
                count.Enter(request.Counted, utcNow);
            }

            if (request.Close || request.Cancel)
            {
                count.Close(access!.User.Id, request.Cancel, utcNow);
            }

            // Closing: the shelf against the stock on hand now, for each supply counted.
            if (request.Close && !request.Cancel)
            {
                // Against the tracked stock row: its row version makes a sale at the same moment clash (review of 5E).
                foreach (OperationsSupplyCountLine line in count.Lines.Where(line => line.Counted is not null))
                {
                    int difference = line.Counted!.Value - ((await _supplies.GetStockAsync(line.SupplyId, warehouse.Id, cancellationToken))?.OnHand ?? 0);

                    if (difference != 0)
                    {
                        await _keeper.MoveAsync(OperationsSupplyMovement.Moved(context.RequiredTenantId, line.SupplyId, warehouse.Id, OperationsSupplyMovementKind.Counted, difference,
                            count.Id, access!.User.Id, null, utcNow), utcNow, cancellationToken);
                        corrections++;
                    }
                }
            }
        }
        catch (DomainException exception)
        {
            return SaveOperationsSupplyCountResponse.Failure(exception.Error);
        }

        if (request.Close || request.Cancel)
        {
            _audit.Record(access!.User.Id, request.Cancel ? "ops.supplies.count_cancelled" : "ops.supplies.count_closed", nameof(OperationsSupplyCount), count.Id,
                after: new { warehouse = warehouse.Code, counted = count.Lines.Count(line => line.Counted is not null), corrections });
        }

        return await _unitOfWork.SaveChangesAsync(cancellationToken)
            ? SaveOperationsSupplyCountResponse.Success(corrections)
            : SaveOperationsSupplyCountResponse.Failure(OperationsCommonErrors.ConcurrencyConflict);
    }
}
