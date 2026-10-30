using Vixly.Operations.Application.Ports;

namespace Vixly.Operations.Infrastructure.Persistence;

// =========================================================
// FILE: OperationsTenantScope.cs
//
// PURPOSE:
// The tenant of the current DI scope (Stage 3, Slice 0, D-F1).
//
// RESPONSIBILITIES:
// - Hold the entered tenant, or the system scope
// - Refuse a second, different tenant in the same scope
//
// IMPORTANT NOTES:
// - Scoped service: one per request, circuit, message or job run
// - Read by OperationsDbContext for its query filter and save check
// =========================================================
public sealed class OperationsTenantScope : IOperationsTenantScope
{
    public Guid? TenantId { get; private set; }

    public bool IsSystem { get; private set; }

    public void Enter(Guid tenantId)
    {
        if (tenantId == Guid.Empty)
        {
            throw new ArgumentException("A tenant id is required.", nameof(tenantId));
        }

        if (IsSystem || (TenantId is { } current && current != tenantId))
        {
            throw new InvalidOperationException("This scope already belongs to another tenant or to the system.");
        }

        TenantId = tenantId;
    }

    public void EnterSystem()
    {
        if (TenantId is not null)
        {
            throw new InvalidOperationException("This scope already belongs to a tenant.");
        }

        IsSystem = true;
    }
}
