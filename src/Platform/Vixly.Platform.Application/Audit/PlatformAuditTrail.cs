using System.Text.Json;
using Vixly.Platform.Application.Ports;
using Vixly.Platform.Domain.Audit;

namespace Vixly.Platform.Application.Audit;

// =========================================================
// FILE: PlatformAuditTrail.cs
//
// PURPOSE:
// Writes audit entries for Platform commands with the
// request's IP, user agent and correlation id.
//
// RESPONSIBILITIES:
// - Serialise arguments and before/after snapshots to JSON
// - Add the entry to the current unit of work, so it commits
//   with the change it records (D-2)
//
// IMPORTANT NOTES:
// - Callers pass plain values only; never secrets
// =========================================================
public sealed class PlatformAuditTrail
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly IPlatformAuditRepository _audit;

    private readonly IPlatformRequestMetadata _metadata;

    private readonly TimeProvider _timeProvider;

    public PlatformAuditTrail(
        IPlatformAuditRepository audit,
        IPlatformRequestMetadata metadata,
        TimeProvider timeProvider)
    {
        _audit = audit ?? throw new ArgumentNullException(nameof(audit));
        _metadata = metadata ?? throw new ArgumentNullException(nameof(metadata));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public void Record(
        Guid? actorUserId,
        string actionCode,
        string? permissionCode,
        string targetType,
        Guid? targetId,
        object? arguments = null,
        object? before = null,
        object? after = null,
        string? reason = null)
    {
        _audit.Add(PlatformAuditEntry.Record(
            new PlatformAuditRecord
            {
                ActorUserId = actorUserId,
                ActionCode = actionCode,
                PermissionCode = permissionCode,
                TargetType = targetType,
                TargetId = targetId,
                ArgumentsJson = Serialize(arguments),
                BeforeJson = Serialize(before),
                AfterJson = Serialize(after),
                Reason = reason,
                IpAddress = _metadata.ClientIp,
                UserAgent = _metadata.UserAgent,
                CorrelationId = _metadata.CorrelationId
            },
            _timeProvider.GetUtcNow()));
    }

    private static string? Serialize(object? value)
    {
        return value is null ? null : JsonSerializer.Serialize(value, JsonOptions);
    }
}
