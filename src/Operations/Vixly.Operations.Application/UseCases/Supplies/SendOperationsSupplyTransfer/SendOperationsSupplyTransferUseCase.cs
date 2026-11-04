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

namespace Vixly.Operations.Application.UseCases.Supplies.SendOperationsSupplyTransfer;

// =========================================================
// FILE: SendOperationsSupplyTransferUseCase.cs
//
// PURPOSE:
// Supplies sent from a warehouse to another of the company's (Slice 5E, S-6): the stock leaves at once and arrives when received.
//
// RESPONSIBILITIES:
// - Run the rule and return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - ops.supplies.stock, for the warehouse concerned (Slice 5E, S-10)
// - Refused while the company does not count its supplies stock (S-9)
// - The movements out carry the form's key; the transfer has its own id
// =========================================================
public sealed class SendOperationsSupplyTransferUseCase
{
    private readonly OperationsSupplyDesk _desk;

    private readonly IOperationsSupplyRepository _supplies;

    private readonly OperationsSupplyStockKeeper _keeper;

    private readonly OperationsAuditTrail _audit;

    private readonly IOperationsUnitOfWork _unitOfWork;

    private readonly TimeProvider _timeProvider;

    private readonly IOperationsLocationRepository _locations;

    public SendOperationsSupplyTransferUseCase(
        OperationsSupplyDesk desk,
        IOperationsSupplyRepository supplies,
        OperationsSupplyStockKeeper keeper,
        OperationsAuditTrail audit,
        IOperationsUnitOfWork unitOfWork,
        TimeProvider timeProvider,
        IOperationsLocationRepository locations)
    {
        _desk = desk ?? throw new ArgumentNullException(nameof(desk));
        _supplies = supplies ?? throw new ArgumentNullException(nameof(supplies));
        _keeper = keeper ?? throw new ArgumentNullException(nameof(keeper));
        _audit = audit ?? throw new ArgumentNullException(nameof(audit));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
        _locations = locations ?? throw new ArgumentNullException(nameof(locations));
    }

    public async Task<SendOperationsSupplyTransferResponse> ExecuteAsync(
        OperationsAuthorizationContext context,
        SendOperationsSupplyTransferRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        (OperationsAccessView? access, IReadOnlyList<OperationsWarehouse> warehouses, OperationsTenantSettings settings, Error? refused) = await _desk.OpenAsync(context, cancellationToken);

        if (refused is not null)
        {
            return SendOperationsSupplyTransferResponse.Failure(refused);
        }

        if (!settings.CountSupplyStock)
        {
            return SendOperationsSupplyTransferResponse.Failure(OperationsSupplyStockErrors.NotCounting);
        }

        if (warehouses.FirstOrDefault(item => item.Id == request.FromWarehouseId) is not { } warehouse)
        {
            return SendOperationsSupplyTransferResponse.Failure(OperationsAccessErrors.PermissionRequired);
        }

        if (request.Key == Guid.Empty)
        {
            return SendOperationsSupplyTransferResponse.Failure(OperationsSupplyStockErrors.LinesInvalid);
        }

        if (await _supplies.LinkUsedAsync(request.Key, cancellationToken))
        {
            return SendOperationsSupplyTransferResponse.Success(request.Key);
        }

        // To any of the company's active warehouses: the person sending keeps the first one's stock only.
        if (await _locations.GetWarehouseAsync(request.ToWarehouseId, cancellationToken) is not { Status: OperationsLocationStatus.Active } destination)
        {
            return SendOperationsSupplyTransferResponse.Failure(OperationsSupplyStockErrors.WarehouseInvalid);
        }

        HashSet<Guid> known = (await _supplies.ListAsync(cancellationToken)).Select(supply => supply.Id).ToHashSet();

        if (request.Lines.Any(line => !known.Contains(line.SupplyId)))
        {
            return SendOperationsSupplyTransferResponse.Failure(OperationsSupplyErrors.NotFound);
        }

        DateTimeOffset utcNow = _timeProvider.GetUtcNow();
        OperationsSupplyTransfer transfer;

        try
        {
            transfer = OperationsSupplyTransfer.Send(context.RequiredTenantId, warehouse.Id, destination.Id, request.Lines, request.Note, access!.User.Id, utcNow);

            foreach (OperationsSupplyLine line in transfer.Lines)
            {
                await _keeper.MoveAsync(OperationsSupplyMovement.Moved(context.RequiredTenantId, line.SupplyId, warehouse.Id, OperationsSupplyMovementKind.TransferOut, line.Quantity,
                    request.Key, access.User.Id, destination.Code, utcNow), utcNow, cancellationToken);
            }
        }
        catch (DomainException exception)
        {
            return SendOperationsSupplyTransferResponse.Failure(exception.Error);
        }

        _supplies.Add(transfer);
        _audit.Record(access.User.Id, "ops.supplies.transfer_sent", nameof(OperationsSupplyTransfer), transfer.Id,
            after: new { from = warehouse.Code, to = destination.Code, lines = transfer.Lines.Count, units = transfer.Lines.Sum(line => line.Quantity) });

        return await _unitOfWork.SaveChangesAsync(cancellationToken)
            ? SendOperationsSupplyTransferResponse.Success(transfer.Id)
            : SendOperationsSupplyTransferResponse.Failure(OperationsCommonErrors.ConcurrencyConflict);
    }
}
