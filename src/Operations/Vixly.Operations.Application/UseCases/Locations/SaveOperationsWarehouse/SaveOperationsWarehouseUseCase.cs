using Vixly.Operations.Application.Audit;
using Vixly.Operations.Application.Ports;
using Vixly.Operations.Application.Security;
using Vixly.Operations.Domain.Common;
using Vixly.Operations.Domain.Locations;
using Vixly.Shared.Kernel.Domain;

namespace Vixly.Operations.Application.UseCases.Locations.SaveOperationsWarehouse;

// =========================================================
// FILE: SaveOperationsWarehouseUseCase.cs
//
// PURPOSE:
// Creates or changes a warehouse (Slice 1B).
//
// RESPONSIBILITIES:
// - Run the rule and return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - A warehouse has its own address and country, so it is listed under that country
// =========================================================
public sealed class SaveOperationsWarehouseUseCase
{
    private readonly IOperationsLocationRepository _locations;

    private readonly OperationsOwnerCheck _owner;

    private readonly OperationsAuditTrail _audit;

    private readonly IOperationsUnitOfWork _unitOfWork;

    private readonly TimeProvider _timeProvider;

    private readonly IOperationsUserRepository _users;

    public SaveOperationsWarehouseUseCase(
        IOperationsUserRepository users,
        IOperationsLocationRepository locations,
        OperationsOwnerCheck owner,
        OperationsAuditTrail audit,
        IOperationsUnitOfWork unitOfWork,
        TimeProvider timeProvider)
    {
        _users = users ?? throw new ArgumentNullException(nameof(users));
        _locations = locations ?? throw new ArgumentNullException(nameof(locations));
        _owner = owner ?? throw new ArgumentNullException(nameof(owner));
        _audit = audit ?? throw new ArgumentNullException(nameof(audit));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public async Task<SaveOperationsWarehouseResponse> ExecuteAsync(
        OperationsAuthorizationContext context,
        SaveOperationsWarehouseRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        if (await _owner.LoadAsync(context, cancellationToken) is not { } access)
        {
            return SaveOperationsWarehouseResponse.Failure(Vixly.Operations.Application.Security.OperationsAccessErrors.SignInRequired);
        }

        OperationsWarehouse? warehouse = request.WarehouseId is { } id ? await _locations.GetWarehouseAsync(id, cancellationToken) : null;

        if ((request.WarehouseId is not null && warehouse is null) || await _locations.GetBranchAsync(request.BranchId, cancellationToken) is null)
        {
            return SaveOperationsWarehouseResponse.Failure(warehouse is null && request.WarehouseId is not null ? OperationsCommonErrors.NotFound : OperationsLocationErrors.BranchRequired);
        }

        // A new warehouse needs its branch in scope; an existing one must be in scope, and moving it needs the new branch in scope too.
        const string manage = Vixly.Operations.Domain.Access.OperationsPermissions.WarehousesManage;
        bool allowed = warehouse is null
            ? access.CoversBranch(manage, request.BranchId)
            : access.CoversWarehouse(manage, warehouse.Id, warehouse.BranchId) && (warehouse.BranchId == request.BranchId || access.CoversBranch(manage, request.BranchId));

        if (!allowed)
        {
            return SaveOperationsWarehouseResponse.Failure(Vixly.Operations.Application.Security.OperationsAccessErrors.PermissionRequired);
        }

        if (await _locations.CodeTakenAsync(request.Code.Trim().ToUpperInvariant(), warehouse?.Id, cancellationToken))
        {
            return SaveOperationsWarehouseResponse.Failure(OperationsLocationErrors.CodeTaken);
        }

        // Slice 4B: the manager is one of the company's active staff, never typed details.
        if (request.ManagerUserId is { } managerId && await _users.GetAsync(managerId, cancellationToken) is not { Account.IsActive: true })
        {
            return SaveOperationsWarehouseResponse.Failure(OperationsDestinationErrors.ManagerInvalid);
        }

        // Slice 4C: the hub is another of the company's warehouses with the hub role, in the same country (the first point after customs).
        if (request.HubWarehouseId is { } hubId && (hubId == request.WarehouseId || await _locations.GetWarehouseAsync(hubId, cancellationToken) is not
            { Status: OperationsLocationStatus.Active } hub || !hub.Roles.HasFlag(OperationsWarehouseRoles.DispatchHub)
            || hub.Address.CountryCode != (request.CountryCode ?? string.Empty).Trim().ToUpperInvariant()))
        {
            return SaveOperationsWarehouseResponse.Failure(OperationsDestinationErrors.HubInvalid);
        }

        // Never a circle of hubs: the chain above the chosen hub must not come back to this warehouse.
        if (request.HubWarehouseId is { } chainStart && request.WarehouseId is { } self && await HubChainReachesAsync(chainStart, self, cancellationToken))
        {
            return SaveOperationsWarehouseResponse.Failure(OperationsDestinationErrors.HubInvalid);
        }

        DateTimeOffset utcNow = _timeProvider.GetUtcNow();

        try
        {
            OperationsLocationDetails details = new(
                request.Code, request.Name, OperationsPostalAddress.Create(request.Line1, request.Line2, request.City, request.Region, request.PostalCode, request.CountryCode),
                request.Phone, request.Email, request.OpeningHours, request.ShowOnSite);

            bool created = warehouse is null;

            if (warehouse is null)
            {
                warehouse = OperationsWarehouse.Create(context.RequiredTenantId, request.BranchId, details, request.Roles, request.AcceptingDeliveries, utcNow);
            }
            else
            {
                warehouse.Update(request.BranchId, details, request.Roles, request.AcceptingDeliveries, request.Status, utcNow);
            }

            warehouse.ChangeOperation(request.TimeZone, request.ManagerUserId, utcNow);
            warehouse.ChangeDestination(new OperationsDestinationTerms(request.DeliversHome, request.HubWarehouseId, request.OperatedBy, request.PartnerName, request.PartnerPhone,
                request.PartnerEmail, request.HomeArea) { ClearsCustoms = request.ClearsCustoms }, utcNow);

            // Added once every rule passed.
            if (created)
            {
                _locations.Add(warehouse);
            }
        }
        catch (DomainException exception)
        {
            return SaveOperationsWarehouseResponse.Failure(exception.Error);
        }

        _audit.Record(context.UserId, request.WarehouseId is null ? "ops.locations.warehouse_created" : "ops.locations.warehouse_changed", nameof(OperationsWarehouse), warehouse.Id,
            after: new { code = warehouse.Code, country = warehouse.Address.CountryCode, roles = warehouse.Roles.ToString(), status = warehouse.Status.ToString(), site = warehouse.ShowOnSite });

        return await _unitOfWork.SaveChangesAsync(cancellationToken)
            ? SaveOperationsWarehouseResponse.Success(warehouse.Id)
            : SaveOperationsWarehouseResponse.Failure(OperationsLocationErrors.CodeTaken);
    }

    private async Task<bool> HubChainReachesAsync(Guid hubId, Guid warehouseId, CancellationToken cancellationToken)
    {
        HashSet<Guid> seen = [];
        Guid? current = hubId;

        while (current is { } id && seen.Add(id))
        {
            if (await _locations.GetWarehouseAsync(id, cancellationToken) is not { } step)
            {
                return false;
            }

            if (step.HubWarehouseId == warehouseId)
            {
                return true;
            }

            current = step.HubWarehouseId;
        }

        return current is not null;
    }
}
