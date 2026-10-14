using Vixly.Shared.Kernel.Domain;

namespace Vixly.Platform.Domain.Tenants;

// =========================================================
// FILE: PlatformTenantLifecycleEvent.cs
//
// PURPOSE:
// One entry of a tenant's history.
//
// RESPONSIBILITIES:
// - Hold the event code and its arguments, the status before and
//   after, the category and reason, the actor and the time
//
// IMPORTANT NOTES:
// - Append-only: nothing changes an event once written
// - Committed with the change it records (D-2)
// - Created only by PlatformTenant, never directly
// - The aggregate does not load its history; each move returns
//   its event and the use case adds it
// =========================================================
public sealed class PlatformTenantLifecycleEvent : AuditableEntity
{
    private PlatformTenantLifecycleEvent()
    {
        EventCode = string.Empty;
        Arguments = new Dictionary<string, string>();
    }

    private PlatformTenantLifecycleEvent(
        Guid platformTenantId,
        string eventCode,
        PlatformTenantStatus? fromStatus,
        PlatformTenantStatus toStatus,
        PlatformSuspensionCategory? category,
        string? reason,
        Guid? actorUserId,
        IReadOnlyDictionary<string, string> arguments,
        DateTimeOffset createdUtc)
        : base(createdUtc)
    {
        PlatformTenantId = platformTenantId;
        EventCode = eventCode;
        FromStatus = fromStatus;
        ToStatus = toStatus;
        Category = category;
        Reason = reason;
        ActorUserId = actorUserId;
        Arguments = new Dictionary<string, string>(arguments, StringComparer.Ordinal);
    }

    public Guid PlatformTenantId { get; private set; }

    public string EventCode { get; private set; }

    public PlatformTenantStatus? FromStatus { get; private set; }

    public PlatformTenantStatus ToStatus { get; private set; }

    public PlatformSuspensionCategory? Category { get; private set; }

    public string? Reason { get; private set; }

    public Guid? ActorUserId { get; private set; }

    public IReadOnlyDictionary<string, string> Arguments { get; private set; }

    internal static PlatformTenantLifecycleEvent Record(
        PlatformTenant tenant,
        string eventCode,
        PlatformTenantStatus? fromStatus,
        Guid? actorUserId,
        DateTimeOffset utcNow,
        PlatformSuspensionCategory? category = null,
        string? reason = null,
        IReadOnlyDictionary<string, string>? arguments = null)
    {
        return new PlatformTenantLifecycleEvent(
            tenant.Id,
            eventCode,
            fromStatus,
            tenant.Status,
            category,
            reason,
            actorUserId,
            arguments ?? new Dictionary<string, string>(),
            utcNow);
    }
}
