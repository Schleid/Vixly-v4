namespace Vixly.Operations.Application.Ports;

// =========================================================
// FILE: IOperationsTenantScope.cs
//
// PURPOSE:
// The tenant the current unit of work belongs to (Stage 3, Slice 0, D-F1).
//
// RESPONSIBILITIES:
// - Enter one tenant, or the system scope for cross-tenant jobs
// - Tell the data layer which tenant to filter by
//
// IMPORTANT NOTES:
// - Entered once per scope: a second, different tenant throws
// - With neither entered, any tenant query throws instead of returning everything
// =========================================================
public interface IOperationsTenantScope
{
    Guid? TenantId { get; }

    bool IsSystem { get; }

    void Enter(Guid tenantId);

    void EnterSystem();
}
