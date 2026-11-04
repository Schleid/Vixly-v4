using Vixly.Operations.Application.Receiving;
using Vixly.Operations.Application.Security;
using Vixly.Operations.Domain.Access;
using Vixly.Operations.Domain.Locations;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.Bundles;

// =========================================================
// FILE: OperationsBundleDesk.cs
//
// PURPOSE:
// Opens bundling for a person (Slice 6A, D-11): the warehouses where they manage or fill bundles.
//
// RESPONSIBILITIES:
// - Refuse a person with neither right anywhere
// - Say what they may do at one warehouse
//
// IMPORTANT NOTES:
// - ops.bundles.manage and ops.bundles.fill are given per warehouse
// =========================================================
public sealed class OperationsBundleDesk
{
    private readonly OperationsOwnerCheck _owner;

    private readonly OperationsReceivingScope _scope;

    public OperationsBundleDesk(OperationsOwnerCheck owner, OperationsReceivingScope scope)
    {
        _owner = owner ?? throw new ArgumentNullException(nameof(owner));
        _scope = scope ?? throw new ArgumentNullException(nameof(scope));
    }

    // The warehouses where the person holds any of these rights.
    public async Task<(OperationsAccessView? Access, IReadOnlyList<OperationsWarehouse> Warehouses, Error? Refused)> OpenAsync(OperationsAuthorizationContext context,
        IReadOnlyList<string> permissions, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(permissions);

        if (await _owner.LoadAsync(context, cancellationToken) is not { } access)
        {
            return (null, Array.Empty<OperationsWarehouse>(), OperationsAccessErrors.SignInRequired);
        }

        Dictionary<Guid, OperationsWarehouse> found = new();

        foreach (string permission in permissions)
        {
            foreach (OperationsWarehouse warehouse in await _scope.WarehousesAsync(access, permission, false, cancellationToken))
            {
                found.TryAdd(warehouse.Id, warehouse);
            }
        }

        List<OperationsWarehouse> warehouses = found.Values.OrderBy(warehouse => warehouse.Code, StringComparer.Ordinal).ToList();
        return warehouses.Count == 0 ? (access, warehouses, OperationsAccessErrors.PermissionRequired) : (access, warehouses, null);
    }

    public static bool Can(OperationsAccessView access, string permission, OperationsWarehouse warehouse)
    {
        ArgumentNullException.ThrowIfNull(access);
        ArgumentNullException.ThrowIfNull(warehouse);
        return access.CoversWarehouse(permission, warehouse.Id, warehouse.BranchId);
    }
}
