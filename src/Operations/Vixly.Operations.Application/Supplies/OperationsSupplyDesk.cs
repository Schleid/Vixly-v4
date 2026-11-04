using Vixly.Operations.Application.Ports;
using Vixly.Operations.Application.Receiving;
using Vixly.Operations.Application.Security;
using Vixly.Operations.Domain.Access;
using Vixly.Operations.Domain.Locations;
using Vixly.Operations.Domain.Settings;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.Supplies;

// =========================================================
// FILE: OperationsSupplyDesk.cs
//
// PURPOSE:
// Opens the supplies stock for a person (Slice 5E step 2): the warehouses they keep the stock of, and the company settings.
//
// RESPONSIBILITIES:
// - Refuse a person who keeps no warehouse's stock
// - Give the company's stock settings
//
// IMPORTANT NOTES:
// - ops.supplies.stock, per warehouse
// - Stock counting off: every stock action is refused with NotCounting by the use cases
// =========================================================
public sealed class OperationsSupplyDesk
{
    private readonly OperationsOwnerCheck _owner;

    private readonly OperationsReceivingScope _scope;

    private readonly IOperationsSettingsRepository _settings;

    public OperationsSupplyDesk(OperationsOwnerCheck owner, OperationsReceivingScope scope, IOperationsSettingsRepository settings)
    {
        _owner = owner ?? throw new ArgumentNullException(nameof(owner));
        _scope = scope ?? throw new ArgumentNullException(nameof(scope));
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
    }

    public async Task<(OperationsAccessView? Access, IReadOnlyList<OperationsWarehouse> Warehouses, OperationsTenantSettings Settings, Error? Refused)> OpenAsync(
        OperationsAuthorizationContext context, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        OperationsTenantSettings settings = await _settings.FindAsync(cancellationToken) ?? OperationsTenantSettings.CreateDefault(context.RequiredTenantId, DateTimeOffset.UtcNow);

        if (await _owner.LoadAsync(context, cancellationToken) is not { } access)
        {
            return (null, Array.Empty<OperationsWarehouse>(), settings, OperationsAccessErrors.SignInRequired);
        }

        IReadOnlyList<OperationsWarehouse> warehouses = await _scope.WarehousesAsync(access, OperationsPermissions.SuppliesStock, false, cancellationToken);
        return warehouses.Count == 0 ? (null, warehouses, settings, OperationsAccessErrors.PermissionRequired) : (access, warehouses, settings, null);
    }
}
