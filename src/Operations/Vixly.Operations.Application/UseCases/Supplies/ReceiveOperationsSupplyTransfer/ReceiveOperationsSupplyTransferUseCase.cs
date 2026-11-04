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

namespace Vixly.Operations.Application.UseCases.Supplies.ReceiveOperationsSupplyTransfer;

// =========================================================
// FILE: ReceiveOperationsSupplyTransferUseCase.cs
//
// PURPOSE:
// Supplies of a transfer received at the warehouse they were sent to (Slice 5E, S-6): the stock arrives as sent.
//
// RESPONSIBILITIES:
// - Run the rule and return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - ops.supplies.stock, for the warehouse it was sent to
// - Refused while the company does not count its supplies stock (S-9)
// =========================================================
public sealed class ReceiveOperationsSupplyTransferUseCase
{
    private readonly OperationsSupplyDesk _desk;

    private readonly IOperationsSupplyRepository _supplies;

    private readonly OperationsSupplyStockKeeper _keeper;

    private readonly OperationsAuditTrail _audit;

    private readonly IOperationsUnitOfWork _unitOfWork;

    private readonly TimeProvider _timeProvider;

    public ReceiveOperationsSupplyTransferUseCase(
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

    public async Task<ReceiveOperationsSupplyTransferResponse> ExecuteAsync(
        OperationsAuthorizationContext context,
        ReceiveOperationsSupplyTransferRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        (OperationsAccessView? access, IReadOnlyList<OperationsWarehouse> warehouses, OperationsTenantSettings settings, Error? refused) = await _desk.OpenAsync(context, cancellationToken);

        if (refused is not null)
        {
            return ReceiveOperationsSupplyTransferResponse.Failure(refused);
        }

        if (!settings.CountSupplyStock)
        {
            return ReceiveOperationsSupplyTransferResponse.Failure(OperationsSupplyStockErrors.NotCounting);
        }

        if (await _supplies.GetTransferAsync(request.TransferId, cancellationToken) is not { } transfer
            || warehouses.FirstOrDefault(item => item.Id == transfer.ToWarehouseId) is not { } warehouse)
        {
            return ReceiveOperationsSupplyTransferResponse.Failure(OperationsSupplyStockErrors.TransferNotFound);
        }

        DateTimeOffset utcNow = _timeProvider.GetUtcNow();

        try
        {
            transfer.Receive(access!.User.Id, utcNow);

            foreach (OperationsSupplyLine line in transfer.Lines)
            {
                await _keeper.MoveAsync(OperationsSupplyMovement.Moved(context.RequiredTenantId, line.SupplyId, warehouse.Id, OperationsSupplyMovementKind.TransferIn, line.Quantity,
                    transfer.Id, access.User.Id, null, utcNow), utcNow, cancellationToken);
            }
        }
        catch (DomainException exception)
        {
            return ReceiveOperationsSupplyTransferResponse.Failure(exception.Error);
        }

        _audit.Record(access.User.Id, "ops.supplies.transfer_received", nameof(OperationsSupplyTransfer), transfer.Id, after: new { warehouse = warehouse.Code });

        // Received twice at once: the transfer's row version lets one through.

        return await _unitOfWork.SaveChangesAsync(cancellationToken)
            ? ReceiveOperationsSupplyTransferResponse.Success()
            : ReceiveOperationsSupplyTransferResponse.Failure(OperationsCommonErrors.ConcurrencyConflict);
    }
}
