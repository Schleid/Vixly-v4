using Vixly.Operations.Application.Audit;
using Vixly.Operations.Application.Ports;
using Vixly.Operations.Application.Receiving;
using Vixly.Operations.Application.Security;
using Vixly.Operations.Domain.Access;
using Vixly.Operations.Domain.Locations;
using Vixly.Operations.Domain.Receiving;
using Vixly.Operations.Domain.Settings;
using Vixly.Operations.Domain.Tracking;
using Vixly.Shared.Kernel.Domain;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Receiving.SaveOperationsStorageLocation;

// =========================================================
// FILE: SaveOperationsStorageLocationUseCase.cs
//
// PURPOSE:
// Adds a shelf to a warehouse, or changes its zone or whether it is in use (Slice 3B).
//
// RESPONSIBILITIES:
// - Run the rule and return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - A code never changes once given: labels and scans refer to it
// - Needs ops.warehouses.manage for that warehouse
// =========================================================
public sealed class SaveOperationsStorageLocationUseCase
{
    private readonly OperationsOwnerCheck _owner;

    private readonly OperationsReceivingScope _scope;

    private readonly IOperationsReceivingRepository _receiving;

    private readonly OperationsAuditTrail _audit;

    private readonly IOperationsUnitOfWork _unitOfWork;

    private readonly TimeProvider _timeProvider;

    public SaveOperationsStorageLocationUseCase(
        OperationsOwnerCheck owner,
        OperationsReceivingScope scope,
        IOperationsReceivingRepository receiving,
        OperationsAuditTrail audit,
        IOperationsUnitOfWork unitOfWork,
        TimeProvider timeProvider)
    {
        _owner = owner ?? throw new ArgumentNullException(nameof(owner));
        _scope = scope ?? throw new ArgumentNullException(nameof(scope));
        _receiving = receiving ?? throw new ArgumentNullException(nameof(receiving));
        _audit = audit ?? throw new ArgumentNullException(nameof(audit));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public async Task<SaveOperationsStorageLocationResponse> ExecuteAsync(
        OperationsAuthorizationContext context,
        SaveOperationsStorageLocationRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        if (await _owner.LoadAsync(context, cancellationToken) is not { } access)
        {
            return SaveOperationsStorageLocationResponse.Failure(OperationsAccessErrors.SignInRequired);
        }

        if (await _scope.CoveredAsync(access, OperationsPermissions.WarehousesManage, request.WarehouseId, cancellationToken) is not { } warehouse)
        {
            return SaveOperationsStorageLocationResponse.Failure(OperationsAccessErrors.PermissionRequired);
        }

        DateTimeOffset utcNow = _timeProvider.GetUtcNow();
        OperationsStorageLocation location;

        try
        {
            if (request.LocationId is { } locationId)
            {
                if (await _receiving.GetLocationAsync(locationId, cancellationToken) is not { } found || found.WarehouseId != warehouse.Id)
                {
                    return SaveOperationsStorageLocationResponse.Failure(OperationsReceivingErrors.LocationInactive);
                }

                location = found;
                location.Change(request.Zone, request.Active, utcNow);
            }
            else
            {
                string code = OperationsStorageLocation.CheckCode(request.Code);

                if (await _receiving.LocationCodeTakenAsync(warehouse.Id, code, null, cancellationToken))
                {
                    return SaveOperationsStorageLocationResponse.Failure(OperationsReceivingErrors.LocationDuplicate);
                }

                location = OperationsStorageLocation.Create(context.RequiredTenantId, warehouse.Id, code, request.Zone, utcNow);
                _receiving.Add(location);
            }
        }
        catch (DomainException exception)
        {
            return SaveOperationsStorageLocationResponse.Failure(exception.Error);
        }

        _audit.Record(access.User.Id, "ops.storage.saved", nameof(OperationsStorageLocation), location.Id,
            after: new { warehouse = warehouse.Code, code = location.Code, zone = location.Zone.ToString(), active = location.IsActive });

        // Two people adding the same code at once: the unique index refuses the second.
        return await _unitOfWork.SaveChangesAsync(cancellationToken)
            ? SaveOperationsStorageLocationResponse.Success(location.Id)
            : SaveOperationsStorageLocationResponse.Failure(OperationsReceivingErrors.LocationDuplicate);
    }
}
