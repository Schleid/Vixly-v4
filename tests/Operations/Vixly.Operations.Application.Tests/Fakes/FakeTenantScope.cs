using Vixly.Operations.Application.Ports;

namespace Vixly.Operations.Application.Tests.Fakes;

// =========================================================
// FILE: FakeTenantScope.cs
//
// PURPOSE:
// Records which tenant a unit of work entered.
//
// RESPONSIBILITIES:
// - Hold the tenant or the system scope
//
// IMPORTANT NOTES:
// - Same rules as the real scope
// =========================================================
internal sealed class FakeTenantScope : IOperationsTenantScope
{
    public Guid? TenantId { get; private set; }

    public bool IsSystem { get; private set; }

    public void Enter(Guid tenantId)
    {
        if (IsSystem || (TenantId is { } current && current != tenantId))
        {
            throw new InvalidOperationException("Scope already entered.");
        }

        TenantId = tenantId;
    }

    public void EnterSystem()
    {
        if (TenantId is not null)
        {
            throw new InvalidOperationException("Scope already entered.");
        }

        IsSystem = true;
    }
}
