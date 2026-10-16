using Vixly.Shared.Kernel.Domain;

namespace Vixly.Platform.Domain.Payments;

// =========================================================
// FILE: PlatformProviderEvent.cs
//
// PURPOSE:
// A webhook event received from the card provider, kept so each is handled once (D-P5).
//
// RESPONSIBILITIES:
// - Hold the provider event id, type and object, when it arrived and how it was handled
//
// IMPORTANT NOTES:
// - The event id is unique: a replayed event is recognised and ignored
// - Only signature-checked events are stored
// =========================================================
public sealed class PlatformProviderEvent : AuditableEntity
{
    private PlatformProviderEvent()
    {
        ProviderEventId = string.Empty;
        EventType = string.Empty;
    }

    private PlatformProviderEvent(PlatformCardProvider provider, string eventId, string eventType, string? objectId, DateTimeOffset utcNow)
        : base(utcNow)
    {
        Provider = provider;
        ProviderEventId = eventId;
        EventType = eventType;
        ObjectId = objectId;
    }

    public PlatformCardProvider Provider { get; private set; }

    public string ProviderEventId { get; private set; }

    public string EventType { get; private set; }

    public string? ObjectId { get; private set; }

    public DateTimeOffset? ProcessedUtc { get; private set; }

    public string? Outcome { get; private set; }

    public static PlatformProviderEvent Receive(PlatformCardProvider provider, string? eventId, string? eventType, string? objectId, DateTimeOffset utcNow)
    {
        string type = eventType?.Trim() ?? string.Empty;

        if (type.Length is 0 or > PlatformPaymentsLimits.EventTypeMaxLength)
        {
            throw new DomainException(PlatformPaymentsErrors.EventInvalid);
        }

        return new PlatformProviderEvent(
            provider,
            PlatformPaymentsLimits.ProviderId(eventId),
            type,
            string.IsNullOrWhiteSpace(objectId) ? null : PlatformPaymentsLimits.ProviderId(objectId),
            utcNow);
    }

    public void MarkProcessed(string outcome, DateTimeOffset utcNow)
    {
        ProcessedUtc = utcNow;
        Outcome = PlatformPaymentsLimits.Code(outcome, PlatformPaymentsLimits.OutcomeMaxLength);
        MarkAsModified(utcNow);
    }
}
