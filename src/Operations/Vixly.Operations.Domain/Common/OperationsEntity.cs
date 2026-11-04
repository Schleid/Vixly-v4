using Vixly.Shared.Kernel.Domain;

namespace Vixly.Operations.Domain.Common;

// =========================================================
// FILE: OperationsEntity.cs
//
// PURPOSE:
// The base of every Operations entity that belongs to a tenant (Stage 3, Slice 0, D-F1).
//
// RESPONSIBILITIES:
// - Carry the tenant id, set once at creation and never changed
//
// IMPORTANT NOTES:
// - The DbContext filters every query and checks every save by this tenant id
// - Only the marked system entities (IOperationsSystemEntity) do without it
// =========================================================
public abstract class OperationsEntity : AuditableEntity
{
    // =====================================================
    // METHOD: ORM CONSTRUCTOR
    // =====================================================
    protected OperationsEntity()
    {
    }

    protected OperationsEntity(Guid tenantId, DateTimeOffset createdUtc)
        : base(createdUtc)
    {
        if (tenantId == Guid.Empty)
        {
            throw new DomainException(OperationsCommonErrors.TenantRequired);
        }

        TenantId = tenantId;
    }

    public Guid TenantId { get; private set; }
}
