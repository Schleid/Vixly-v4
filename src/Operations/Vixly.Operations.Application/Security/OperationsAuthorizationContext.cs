namespace Vixly.Operations.Application.Security;

// =========================================================
// FILE: OperationsAuthorizationContext.cs
//
// PURPOSE:
// Who is acting in Operations: the tenant from the host and, from Slice 1, the signed-in user (S-2).
//
// RESPONSIBILITIES:
// - Carry the trusted tenant, user and session ids into every use case
//
// IMPORTANT NOTES:
// - Built only from the resolved host and the server-side session, never from a request body (S-2)
// - Host has no tenant: only host resolution runs with it
// =========================================================
public sealed record OperationsAuthorizationContext
{
    private OperationsAuthorizationContext(Guid? tenantId, Guid? userId, Guid? sessionId, bool isSettingUp = false)
    {
        TenantId = tenantId;
        UserId = userId;
        SessionId = sessionId;
        IsSettingUp = isSettingUp;
    }

    // Resolving which tenant a host name belongs to happens before any tenant is known.
    public static OperationsAuthorizationContext Host { get; } = new(null, null, null);

    public Guid? TenantId { get; }

    public Guid? UserId { get; }

    public Guid? SessionId { get; }

    // The tenant is still being set up: only the owner setup pages work, and sessions are setup sessions (Slice 1, D-O3).
    public bool IsSettingUp { get; }

    public Guid RequiredTenantId => TenantId ?? throw new InvalidOperationException("This use case needs a tenant.");

    public static OperationsAuthorizationContext ForTenant(Guid tenantId, bool settingUp = false)
    {
        return tenantId == Guid.Empty
            ? throw new ArgumentException("A tenant id is required.", nameof(tenantId))
            : new OperationsAuthorizationContext(tenantId, null, null, settingUp);
    }

    public OperationsAuthorizationContext WithUser(Guid userId, Guid sessionId)
    {
        return TenantId is null || userId == Guid.Empty || sessionId == Guid.Empty
            ? throw new InvalidOperationException("A signed-in user needs a tenant, a user and a session.")
            : new OperationsAuthorizationContext(TenantId, userId, sessionId, IsSettingUp);
    }
}
