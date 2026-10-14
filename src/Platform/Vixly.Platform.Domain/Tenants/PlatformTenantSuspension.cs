using Vixly.Shared.Kernel.Domain;

namespace Vixly.Platform.Domain.Tenants;

// =========================================================
// FILE: PlatformTenantSuspension.cs
//
// PURPOSE:
// One manual suspension of a tenant: why, by whom, and when it
// was lifted (D-T2).
//
// RESPONSIBILITIES:
// - Hold the category and the internal reason
// - Record who lifted it and why
//
// IMPORTANT NOTES:
// - Owned by PlatformTenant; at most one is open
// - The reason is internal: notices show the category only
// =========================================================
public sealed class PlatformTenantSuspension : AuditableEntity
{
    private PlatformTenantSuspension()
    {
        Reason = string.Empty;
    }

    private PlatformTenantSuspension(
        Guid platformTenantId,
        PlatformSuspensionCategory category,
        string reason,
        Guid suspendedByUserId,
        DateTimeOffset createdUtc)
        : base(createdUtc)
    {
        PlatformTenantId = platformTenantId;
        Category = category;
        Reason = reason;
        SuspendedByUserId = suspendedByUserId;
    }

    public Guid PlatformTenantId { get; private set; }

    public PlatformSuspensionCategory Category { get; private set; }

    public string Reason { get; private set; }

    public Guid SuspendedByUserId { get; private set; }

    public Guid? ResolvedByUserId { get; private set; }

    public DateTimeOffset? ResolvedUtc { get; private set; }

    public string? ResolutionReason { get; private set; }

    public bool IsOpen => ResolvedUtc is null;

    internal static PlatformTenantSuspension Open(
        Guid platformTenantId,
        PlatformSuspensionCategory category,
        string reason,
        Guid suspendedByUserId,
        DateTimeOffset utcNow)
    {
        return new PlatformTenantSuspension(platformTenantId, category, reason, suspendedByUserId, utcNow);
    }

    internal void Resolve(Guid resolvedByUserId, string reason, DateTimeOffset utcNow)
    {
        ResolvedByUserId = resolvedByUserId;
        ResolutionReason = reason;
        ResolvedUtc = utcNow;
        MarkAsModified(utcNow);
    }
}
