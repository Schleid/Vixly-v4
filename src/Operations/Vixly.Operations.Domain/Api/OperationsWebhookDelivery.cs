using Vixly.Operations.Domain.Common;

namespace Vixly.Operations.Domain.Api;

// =========================================================
// FILE: OperationsWebhookDelivery.cs
//
// PURPOSE:
// One event on its way to one webhook address (Slice 10).
//
// RESPONSIBILITIES:
// - Keep the event (id, name, body) and how its tries went
// - Try again later on a failure (1 minute to 10 hours, about a day and a half in all), then give up; sent again by hand
//
// IMPORTANT NOTES:
// - At least once: the receiver uses the event id to ignore a repeat
// - Only the answer’s status is kept, never its body
// =========================================================
public sealed class OperationsWebhookDelivery : OperationsEntity
{
    public const int EventIdMaxLength = 64;

    public const int EventTypeMaxLength = 40;

    public const int PayloadMaxLength = 64 * 1024;

    public const int ErrorMaxLength = 64;

    // The wait after each failed try; after the last, the delivery has failed.
    public static IReadOnlyList<TimeSpan> Backoff { get; } = new[]
    {
        TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(30), TimeSpan.FromHours(2), TimeSpan.FromHours(5), TimeSpan.FromHours(10),
        TimeSpan.FromHours(10), TimeSpan.FromHours(10)
    };

    private OperationsWebhookDelivery()
    {
        EventId = string.Empty;
        EventType = string.Empty;
        Payload = string.Empty;
    }

    private OperationsWebhookDelivery(Guid tenantId, DateTimeOffset utcNow)
        : base(tenantId, utcNow)
    {
        EventId = string.Empty;
        EventType = string.Empty;
        Payload = string.Empty;
    }

    public Guid EndpointId { get; private set; }

    public string EventId { get; private set; }

    public string EventType { get; private set; }

    public string Payload { get; private set; }

    public OperationsWebhookStatus Status { get; private set; }

    public int Attempts { get; private set; }

    public DateTimeOffset NextAttemptUtc { get; private set; }

    public DateTimeOffset? LastAttemptUtc { get; private set; }

    public int? LastStatusCode { get; private set; }

    public string? LastError { get; private set; }

    public DateTimeOffset? LeaseUntilUtc { get; private set; }

    // Who holds the lease: only that sender may record the result.
    public Guid? LeaseToken { get; private set; }

    // A test sent by a person: tried once, never again on its own, and not counted against the address.
    public bool IsTest => EventType == OperationsWebhookEventNames.Test;

    public static OperationsWebhookDelivery Create(Guid tenantId, Guid endpointId, string eventId, string eventType, string payload, DateTimeOffset utcNow)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(eventId);
        ArgumentException.ThrowIfNullOrWhiteSpace(eventType);
        ArgumentNullException.ThrowIfNull(payload);

        if (eventId.Length > EventIdMaxLength || eventType.Length > EventTypeMaxLength || payload.Length > PayloadMaxLength)
        {
            throw new ArgumentOutOfRangeException(nameof(payload));
        }

        return new OperationsWebhookDelivery(tenantId, utcNow)
        {
            EndpointId = endpointId,
            EventId = eventId,
            EventType = eventType,
            Payload = payload,
            Status = OperationsWebhookStatus.Pending,
            NextAttemptUtc = utcNow
        };
    }

    public bool Due(DateTimeOffset utcNow) => Status == OperationsWebhookStatus.Pending && NextAttemptUtc <= utcNow && (LeaseUntilUtc is null || LeaseUntilUtc <= utcNow);

    public void Lease(Guid token, DateTimeOffset until, DateTimeOffset utcNow)
    {
        (LeaseToken, LeaseUntilUtc) = (token, until);
        MarkAsModified(utcNow);
    }

    public void Succeeded(int statusCode, DateTimeOffset utcNow)
    {
        Attempts++;
        (Status, LastAttemptUtc, LastStatusCode, LastError, LeaseUntilUtc, LeaseToken) = (OperationsWebhookStatus.Delivered, utcNow, statusCode, null, null, null);
        MarkAsModified(utcNow);
    }

    // A failed try: the next one is planned, or the delivery has failed for good (true).
    public bool Attempted(int? statusCode, string? error, DateTimeOffset utcNow)
    {
        Attempts++;
        (LastAttemptUtc, LastStatusCode, LeaseUntilUtc, LeaseToken) = (utcNow, statusCode, null, null);
        LastError = error is { Length: > ErrorMaxLength } ? error[..ErrorMaxLength] : error;

        if (Attempts > Backoff.Count || IsTest)
        {
            Status = OperationsWebhookStatus.Failed;
        }
        else
        {
            NextAttemptUtc = utcNow + Backoff[Attempts - 1];
        }

        MarkAsModified(utcNow);
        return Status == OperationsWebhookStatus.Failed;
    }

    // Sent again by a person: a fresh series of tries, starting now.
    public void Resend(DateTimeOffset utcNow)
    {
        (Status, Attempts, NextAttemptUtc, LeaseUntilUtc, LeaseToken) = (OperationsWebhookStatus.Pending, 0, utcNow, null, null);
        MarkAsModified(utcNow);
    }
}
