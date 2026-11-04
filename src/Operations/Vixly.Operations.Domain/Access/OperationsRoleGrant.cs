using Vixly.Operations.Domain.Common;
using Vixly.Shared.Kernel.Domain;

namespace Vixly.Operations.Domain.Access;

// =========================================================
// FILE: OperationsRoleGrant.cs
//
// PURPOSE:
// A role given to a staff member for the whole tenant, one branch or one warehouse (Slice 1D).
//
// RESPONSIBILITIES:
// - Record who granted what, where and when
// - Revoke, keeping the record
//
// IMPORTANT NOTES:
// - Never deleted: a revoked grant stays as history
// =========================================================
public sealed class OperationsRoleGrant : OperationsEntity
{
    private OperationsRoleGrant()
    {
    }

    private OperationsRoleGrant(Guid tenantId, DateTimeOffset createdUtc)
        : base(tenantId, createdUtc)
    {
    }

    public Guid UserId { get; private set; }

    public Guid RoleId { get; private set; }

    public OperationsRoleScope Scope { get; private set; }

    public Guid? ScopeId { get; private set; }

    public Guid? GrantedBy { get; private set; }

    public DateTimeOffset GrantedUtc { get; private set; }

    public Guid? RevokedBy { get; private set; }

    public DateTimeOffset? RevokedUtc { get; private set; }

    public bool IsActive => RevokedUtc is null;

    public static OperationsRoleGrant Grant(Guid tenantId, Guid userId, Guid roleId, OperationsRoleScope scope, Guid? scopeId, Guid? grantedBy, DateTimeOffset utcNow)
    {
        if (userId == Guid.Empty || roleId == Guid.Empty || !Enum.IsDefined(scope) || (scope == OperationsRoleScope.Tenant) != (scopeId is null) || scopeId == Guid.Empty)
        {
            throw new DomainException(OperationsRoleErrors.ScopeInvalid);
        }

        return new OperationsRoleGrant(tenantId, utcNow)
        {
            UserId = userId,
            RoleId = roleId,
            Scope = scope,
            ScopeId = scopeId,
            GrantedBy = grantedBy,
            GrantedUtc = utcNow
        };
    }

    public void Revoke(Guid? revokedBy, DateTimeOffset utcNow)
    {
        if (RevokedUtc is not null)
        {
            return;
        }

        RevokedBy = revokedBy;
        RevokedUtc = utcNow;
        MarkAsModified(utcNow);
    }
}
