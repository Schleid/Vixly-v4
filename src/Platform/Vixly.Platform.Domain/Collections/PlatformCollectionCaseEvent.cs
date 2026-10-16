using Vixly.Shared.Kernel.Domain;

namespace Vixly.Platform.Domain.Collections;

// =========================================================
// FILE: PlatformCollectionCaseEvent.cs
//
// PURPOSE:
// One line of a collections case's history.
//
// RESPONSIBILITIES:
// - Record what happened, who did it and the values that matter (codes and ids only)
// - Carry the retry key that makes a card retry happen once per invoice and day
//
// IMPORTANT NOTES:
// - Append-only: the app login may not update or delete these rows
// - A note is internal; it never reaches the tenant
// =========================================================
public sealed class PlatformCollectionCaseEvent : AuditableEntity
{
    private PlatformCollectionCaseEvent()
    {
        EventCode = string.Empty;
        Arguments = new Dictionary<string, string>(StringComparer.Ordinal);
    }

    private PlatformCollectionCaseEvent(Guid caseId, Guid tenantId, string eventCode, Guid? actorUserId, string? note, string? retryKey, IReadOnlyDictionary<string, string> arguments, DateTimeOffset utcNow)
        : base(utcNow)
    {
        PlatformCollectionCaseId = caseId;
        PlatformTenantId = tenantId;
        EventCode = eventCode;
        ActorUserId = actorUserId;
        Note = note;
        RetryKey = retryKey;
        Arguments = arguments;
    }

    public Guid PlatformCollectionCaseId { get; private set; }

    public Guid PlatformTenantId { get; private set; }

    public string EventCode { get; private set; }

    public Guid? ActorUserId { get; private set; }

    public string? Note { get; private set; }

    public string? RetryKey { get; private set; }

    public IReadOnlyDictionary<string, string> Arguments { get; private set; }

    internal static PlatformCollectionCaseEvent Record(
        PlatformCollectionCase collectionCase,
        string eventCode,
        Guid? actorUserId,
        DateTimeOffset utcNow,
        string? note = null,
        IReadOnlyDictionary<string, string>? arguments = null,
        string? retryKey = null)
    {
        return new PlatformCollectionCaseEvent(
            collectionCase.Id,
            collectionCase.PlatformTenantId,
            eventCode,
            actorUserId,
            note,
            retryKey,
            arguments ?? new Dictionary<string, string>(StringComparer.Ordinal),
            utcNow);
    }

    public static string Stamp(DateTimeOffset value)
    {
        return value.UtcDateTime.ToString("yyyy-MM-ddTHH:mm:ssZ", System.Globalization.CultureInfo.InvariantCulture);
    }
}
