using Vixly.Shared.Kernel.Domain;

namespace Vixly.Platform.Domain.Licensing;

// =========================================================
// FILE: PlatformDeviceEvent.cs
//
// PURPOSE:
// One line of a device's or a tenant's licensing history (D-L14).
//
// RESPONSIBILITIES:
// - Record what happened, who did it and the values that matter
//
// IMPORTANT NOTES:
// - Append-only: the app login may not update or delete these rows
// - Device is empty for tenant-level lines (a link sent)
// =========================================================
public sealed class PlatformDeviceEvent : AuditableEntity
{
    private PlatformDeviceEvent()
    {
        EventCode = string.Empty;
        Arguments = new Dictionary<string, string>(StringComparer.Ordinal);
    }

    private PlatformDeviceEvent(Guid tenantId, Guid? deviceId, string eventCode, PlatformDeviceActor actor, PlatformDeviceReason? reason, string? note, IReadOnlyDictionary<string, string> arguments, DateTimeOffset utcNow)
        : base(utcNow)
    {
        PlatformTenantId = tenantId;
        PlatformDeviceId = deviceId;
        EventCode = eventCode;
        ActorReference = actor.Reference;
        ActorName = actor.Name;
        ActorUserId = actor.StaffUserId;
        Reason = reason;
        Note = note;
        Arguments = arguments;
    }

    public Guid PlatformTenantId { get; private set; }

    public Guid? PlatformDeviceId { get; private set; }

    public string EventCode { get; private set; }

    public string? ActorReference { get; private set; }

    public string? ActorName { get; private set; }

    public Guid? ActorUserId { get; private set; }

    public PlatformDeviceReason? Reason { get; private set; }

    public string? Note { get; private set; }

    public IReadOnlyDictionary<string, string> Arguments { get; private set; }

    public static PlatformDeviceEvent Record(
        Guid tenantId,
        Guid? deviceId,
        string eventCode,
        PlatformDeviceActor actor,
        DateTimeOffset utcNow,
        PlatformDeviceReason? reason = null,
        string? note = null,
        IReadOnlyDictionary<string, string>? arguments = null)
    {
        ArgumentNullException.ThrowIfNull(actor);

        return new PlatformDeviceEvent(tenantId, deviceId, eventCode, actor, reason, note, arguments ?? new Dictionary<string, string>(StringComparer.Ordinal), utcNow);
    }
}
