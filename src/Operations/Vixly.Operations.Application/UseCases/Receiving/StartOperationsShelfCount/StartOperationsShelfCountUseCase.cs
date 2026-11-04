using Vixly.Operations.Application.Audit;
using Vixly.Operations.Application.Ports;
using Vixly.Operations.Application.Receiving;
using Vixly.Operations.Application.Security;
using Vixly.Operations.Domain.Access;
using Vixly.Operations.Domain.Common;
using Vixly.Operations.Domain.Locations;
using Vixly.Operations.Domain.Receiving;
using Vixly.Shared.Kernel.Domain;

namespace Vixly.Operations.Application.UseCases.Receiving.StartOperationsShelfCount;

// =========================================================
// FILE: StartOperationsShelfCountUseCase.cs
//
// PURPOSE:
// Starts the count of one shelf, by its scanned code (Slice 12D, owner H-7); an open count of that shelf goes on.
//
// RESPONSIBILITIES:
// - Run the rule and return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - Needs Receive at the shelf's warehouse
// - One open count per shelf: starting again continues it
// =========================================================
public sealed class StartOperationsShelfCountUseCase
{
    private readonly OperationsOwnerCheck _owner;

    private readonly OperationsReceivingScope _scope;

    private readonly IOperationsShelfCountRepository _counts;

    private readonly IOperationsReceivingRepository _receiving;

    private readonly OperationsAuditTrail _audit;

    private readonly IOperationsUnitOfWork _unitOfWork;

    private readonly TimeProvider _timeProvider;

    public StartOperationsShelfCountUseCase(
        OperationsOwnerCheck owner,
        OperationsReceivingScope scope,
        IOperationsShelfCountRepository counts,
        IOperationsReceivingRepository receiving,
        OperationsAuditTrail audit,
        IOperationsUnitOfWork unitOfWork,
        TimeProvider timeProvider)
    {
        _owner = owner ?? throw new ArgumentNullException(nameof(owner));
        _scope = scope ?? throw new ArgumentNullException(nameof(scope));
        _counts = counts ?? throw new ArgumentNullException(nameof(counts));
        _receiving = receiving ?? throw new ArgumentNullException(nameof(receiving));
        _audit = audit ?? throw new ArgumentNullException(nameof(audit));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public async Task<StartOperationsShelfCountResponse> ExecuteAsync(
        OperationsAuthorizationContext context,
        StartOperationsShelfCountRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        if (await _owner.LoadAsync(context, cancellationToken) is not { } access)
        {
            return StartOperationsShelfCountResponse.Failure(OperationsAccessErrors.SignInRequired);
        }

        string code = (request.ShelfCode ?? string.Empty).Trim();
        OperationsStorageLocation? shelf = null;
        OperationsWarehouse? here = null;

        foreach (OperationsWarehouse warehouse in await _scope.WarehousesAsync(access, OperationsPermissions.ReceivingReceive, false, cancellationToken))
        {
            if ((request.WarehouseId is null || request.WarehouseId == warehouse.Id)
                && (await _receiving.ListLocationsAsync(warehouse.Id, cancellationToken)).FirstOrDefault(item => item.IsActive && string.Equals(item.Code, code, StringComparison.OrdinalIgnoreCase)) is { } found)
            {
                (shelf, here) = (found, warehouse);
                break;
            }
        }

        if (shelf is null || here is null)
        {
            return StartOperationsShelfCountResponse.Failure(OperationsReceivingErrors.ShelfUnknown);
        }

        if (await _counts.FindOpenAsync(shelf.Id, cancellationToken) is { } open)
        {
            return StartOperationsShelfCountResponse.Success(open.Id);
        }

        OperationsShelfCount count = OperationsShelfCount.Start(context.RequiredTenantId, here.Id, shelf, access.User.Id, _timeProvider.GetUtcNow());
        _counts.Add(count);
        _audit.Record(access.User.Id, "ops.parcels.shelf_count_started", nameof(OperationsShelfCount), count.Id, arguments: new { shelf = shelf.Code });

        return await _unitOfWork.SaveChangesAsync(cancellationToken)
            ? StartOperationsShelfCountResponse.Success(count.Id)
            : StartOperationsShelfCountResponse.Failure(OperationsCommonErrors.ConcurrencyConflict);
    }
}
