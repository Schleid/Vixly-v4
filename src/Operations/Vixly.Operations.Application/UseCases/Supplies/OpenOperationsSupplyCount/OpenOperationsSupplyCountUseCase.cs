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

namespace Vixly.Operations.Application.UseCases.Supplies.OpenOperationsSupplyCount;

// =========================================================
// FILE: OpenOperationsSupplyCountUseCase.cs
//
// PURPOSE:
// A stock count opened at a warehouse (Slice 5E, S-7): every active supply, or one category.
//
// RESPONSIBILITIES:
// - Run the rule and return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - ops.supplies.stock, for the warehouse concerned (Slice 5E, S-10)
// - Refused while the company does not count its supplies stock (S-9)
// =========================================================
public sealed class OpenOperationsSupplyCountUseCase
{
    private readonly OperationsSupplyDesk _desk;

    private readonly IOperationsSupplyRepository _supplies;

    private readonly OperationsAuditTrail _audit;

    private readonly IOperationsUnitOfWork _unitOfWork;

    private readonly TimeProvider _timeProvider;

    public OpenOperationsSupplyCountUseCase(
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

    public async Task<OpenOperationsSupplyCountResponse> ExecuteAsync(
        OperationsAuthorizationContext context,
        OpenOperationsSupplyCountRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        (OperationsAccessView? access, IReadOnlyList<OperationsWarehouse> warehouses, OperationsTenantSettings settings, Error? refused) = await _desk.OpenAsync(context, cancellationToken);

        if (refused is not null)
        {
            return OpenOperationsSupplyCountResponse.Failure(refused);
        }

        if (!settings.CountSupplyStock)
        {
            return OpenOperationsSupplyCountResponse.Failure(OperationsSupplyStockErrors.NotCounting);
        }

        if (warehouses.FirstOrDefault(item => item.Id == request.WarehouseId) is not { } warehouse)
        {
            return OpenOperationsSupplyCountResponse.Failure(OperationsAccessErrors.PermissionRequired);
        }

        // One open count per warehouse: opened twice (a double click), the one already open.
        if (await _supplies.FindOpenCountAsync(warehouse.Id, cancellationToken) is { } open)
        {
            return OpenOperationsSupplyCountResponse.Success(open.Id);
        }

        List<Guid> supplies = (await _supplies.ListAsync(cancellationToken))
            .Where(supply => supply.IsActive && (request.Category is null || supply.Category == request.Category)).Select(supply => supply.Id).ToList();
        OperationsSupplyCount count;

        try
        {
            count = OperationsSupplyCount.Open(context.RequiredTenantId, warehouse.Id, supplies, access!.User.Id, _timeProvider.GetUtcNow());
        }
        catch (DomainException exception)
        {
            return OpenOperationsSupplyCountResponse.Failure(exception.Error);
        }

        _supplies.Add(count);
        _audit.Record(access.User.Id, "ops.supplies.count_opened", nameof(OperationsSupplyCount), count.Id,
            after: new { warehouse = warehouse.Code, category = request.Category?.ToString(), supplies = supplies.Count });

        return await _unitOfWork.SaveChangesAsync(cancellationToken)
            ? OpenOperationsSupplyCountResponse.Success(count.Id)
            : OpenOperationsSupplyCountResponse.Failure(OperationsCommonErrors.ConcurrencyConflict);
    }
}
