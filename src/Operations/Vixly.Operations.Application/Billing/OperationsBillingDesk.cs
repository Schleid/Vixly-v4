using Vixly.Operations.Application.Ports;
using Vixly.Operations.Application.Receiving;
using Vixly.Operations.Application.Security;
using Vixly.Operations.Domain.Access;
using Vixly.Operations.Domain.Locations;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.Billing;

// =========================================================
// FILE: OperationsBillingDesk.cs
//
// PURPOSE:
// Opens invoices for a person (Slice 5F, I-8 and I-9): the warehouses where they hold a right about invoices.
//
// RESPONSIBILITIES:
// - Refuse a person without the right in any warehouse
// - Say whether they hold a right at one invoice's warehouse
//
// IMPORTANT NOTES:
// - ops.invoices.view, ops.credits.request and ops.credits.approve are given per warehouse (the invoice's)
// =========================================================
public sealed class OperationsBillingDesk
{
    private readonly OperationsOwnerCheck _owner;

    private readonly OperationsReceivingScope _scope;

    public OperationsBillingDesk(OperationsOwnerCheck owner, OperationsReceivingScope scope)
    {
        _owner = owner ?? throw new ArgumentNullException(nameof(owner));
        _scope = scope ?? throw new ArgumentNullException(nameof(scope));
    }

    public async Task<(OperationsAccessView? Access, IReadOnlyList<OperationsWarehouse> Warehouses, Error? Refused)> OpenAsync(OperationsAuthorizationContext context, string permission,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (await _owner.LoadAsync(context, cancellationToken) is not { } access)
        {
            return (null, Array.Empty<OperationsWarehouse>(), OperationsAccessErrors.SignInRequired);
        }

        IReadOnlyList<OperationsWarehouse> warehouses = await _scope.WarehousesAsync(access, permission, false, cancellationToken);
        return warehouses.Count == 0 ? (access, warehouses, OperationsAccessErrors.PermissionRequired) : (access, warehouses, null);
    }

    public async Task<IReadOnlyList<OperationsWarehouse>> WarehousesAsync(OperationsAccessView access, string permission, CancellationToken cancellationToken) =>
        await _scope.WarehousesAsync(access ?? throw new ArgumentNullException(nameof(access)), permission, false, cancellationToken);
}
