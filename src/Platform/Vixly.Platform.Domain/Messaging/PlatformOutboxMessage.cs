using System.Text.RegularExpressions;
using Vixly.Shared.Kernel.Domain;

namespace Vixly.Platform.Domain.Messaging;

// =========================================================
// FILE: PlatformOutboxMessage.cs
//
// PURPOSE:
// A fact that must reach its handlers, saved in the same commit
// as the change it describes (D-T17).
//
// RESPONSIBILITIES:
// - Hold the versioned type, the stream it orders within, the JSON
//   payload and the delivery state
// - Record success, or failure with back-off, until it is set
//   aside after too many attempts
//
// IMPORTANT NOTES:
// - Delivery is at least once; handlers are idempotent by Id
// - Messages of one stream (a tenant) are delivered in order
// - The payload is written by the Application with the
//   serializer; the domain only checks it is present
// - A dispatcher leases a message before handling it, so two
//   dispatchers never take the same one
// - Back-off doubles from 30 seconds up to one hour; after 10
//   failures the message waits for an operator
// =========================================================
public sealed class PlatformOutboxMessage : AuditableEntity
{
    public const int TypeMaxLength = 100;

    public const int ErrorCodeMaxLength = 100;

    public const int MaxAttempts = 10;

    private static readonly Regex TypePattern = new(
        "^[a-z][a-z0-9_.]*\\.v[0-9]+$",
        RegexOptions.CultureInvariant | RegexOptions.Compiled);

    private PlatformOutboxMessage()
    {
        Type = string.Empty;
        Payload = string.Empty;
    }

    private PlatformOutboxMessage(string type, Guid streamId, string payload, DateTimeOffset createdUtc)
        : base(createdUtc)
    {
        Type = type;
        StreamId = streamId;
        Payload = payload;
        NextAttemptUtc = createdUtc;
    }

    public string Type { get; private set; }

    public Guid StreamId { get; private set; }

    public string Payload { get; private set; }

    public DateTimeOffset? ProcessedUtc { get; private set; }

    public int Attempts { get; private set; }

    public DateTimeOffset? NextAttemptUtc { get; private set; }

    public string? LastErrorCode { get; private set; }

    public DateTimeOffset? LeasedUntilUtc { get; private set; }

    public bool IsSetAside => ProcessedUtc is null && NextAttemptUtc is null;

    // =====================================================
    // METHOD: CREATE
    // =====================================================
    public static PlatformOutboxMessage Create(string type, Guid streamId, string payload, DateTimeOffset utcNow)
    {
        if (string.IsNullOrEmpty(type) || type.Length > TypeMaxLength || !TypePattern.IsMatch(type))
        {
            throw new DomainException(PlatformMessagingErrors.TypeInvalid);
        }

        if (streamId == Guid.Empty)
        {
            throw new DomainException(PlatformMessagingErrors.StreamRequired);
        }

        if (string.IsNullOrWhiteSpace(payload))
        {
            throw new DomainException(PlatformMessagingErrors.PayloadRequired);
        }

        return new PlatformOutboxMessage(type, streamId, payload, utcNow);
    }

    // =====================================================
    // METHOD: DELIVERY
    // =====================================================
    public void Lease(DateTimeOffset leasedUntilUtc, DateTimeOffset utcNow)
    {
        LeasedUntilUtc = leasedUntilUtc;
        MarkAsModified(utcNow);
    }

    public void MarkProcessed(DateTimeOffset utcNow)
    {
        Attempts++;
        ProcessedUtc = utcNow;
        NextAttemptUtc = null;
        LeasedUntilUtc = null;
        LastErrorCode = null;
        MarkAsModified(utcNow);
    }

    public void MarkFailed(string errorCode, DateTimeOffset utcNow)
    {
        Attempts++;
        LastErrorCode = string.IsNullOrEmpty(errorCode) || errorCode.Length > ErrorCodeMaxLength
            ? "platform.messaging.handler_failed"
            : errorCode;
        NextAttemptUtc = Attempts >= MaxAttempts ? null : utcNow.Add(BackOff(Attempts));
        LeasedUntilUtc = null;
        MarkAsModified(utcNow);
    }

    public void Retry(DateTimeOffset utcNow)
    {
        if (!IsSetAside)
        {
            return;
        }

        Attempts = 0;
        NextAttemptUtc = utcNow;
        MarkAsModified(utcNow);
    }

    public static TimeSpan BackOff(int attempts)
    {
        double seconds = 30 * Math.Pow(2, Math.Max(0, attempts - 1));

        return TimeSpan.FromSeconds(Math.Min(seconds, TimeSpan.FromHours(1).TotalSeconds));
    }
}
