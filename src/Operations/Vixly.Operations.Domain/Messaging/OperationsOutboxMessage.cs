using System.Text.RegularExpressions;
using Vixly.Operations.Domain.Common;
using Vixly.Shared.Kernel.Domain;

namespace Vixly.Operations.Domain.Messaging;

// =========================================================
// FILE: OperationsOutboxMessage.cs
//
// PURPOSE:
// A message written in the same commit as the change that caused it, delivered later (D-2, as Platform).
//
// RESPONSIBILITIES:
// - Hold the type, stream, payload and delivery state
// - Back off after a failure and stop after 10 attempts
//
// IMPORTANT NOTES:
// - A system entity: the dispatcher reads it across tenants; TenantId records whom it concerns
// - Messages of one stream are delivered in order
// =========================================================
public sealed class OperationsOutboxMessage : AuditableEntity, IOperationsSystemEntity
{
    public const int TypeMaxLength = 100;

    public const int ErrorCodeMaxLength = 100;

    public const int MaxAttempts = 10;

    private static readonly Regex TypePattern = new(
        "^ops\\.[a-z0-9_.]+\\.v[0-9]+$",
        RegexOptions.CultureInvariant | RegexOptions.Compiled);

    private OperationsOutboxMessage()
    {
        Type = string.Empty;
        Payload = string.Empty;
    }

    private OperationsOutboxMessage(string type, Guid? tenantId, Guid streamId, string payload, DateTimeOffset createdUtc)
        : base(createdUtc)
    {
        Type = type;
        TenantId = tenantId;
        StreamId = streamId;
        Payload = payload;
        NextAttemptUtc = createdUtc;
    }

    public string Type { get; private set; }

    public Guid? TenantId { get; private set; }

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
    public static OperationsOutboxMessage Create(string type, Guid? tenantId, Guid streamId, string payload, DateTimeOffset utcNow)
    {
        if (string.IsNullOrEmpty(type) || type.Length > TypeMaxLength || !TypePattern.IsMatch(type))
        {
            throw new DomainException(OperationsMessagingErrors.TypeInvalid);
        }

        if (streamId == Guid.Empty)
        {
            throw new DomainException(OperationsMessagingErrors.StreamRequired);
        }

        if (string.IsNullOrWhiteSpace(payload))
        {
            throw new DomainException(OperationsMessagingErrors.PayloadRequired);
        }

        return new OperationsOutboxMessage(type, tenantId, streamId, payload, utcNow);
    }

    // =====================================================
    // METHOD: DELIVERED
    // =====================================================
    public void MarkProcessed(DateTimeOffset utcNow)
    {
        ProcessedUtc = utcNow;
        NextAttemptUtc = null;
        LeasedUntilUtc = null;
        LastErrorCode = null;
        MarkAsModified(utcNow);
    }

    // =====================================================
    // METHOD: FAILED
    // =====================================================
    // Backs off 1, 2, 4 ... minutes (at most an hour); after MaxAttempts the message is set aside for a person.
    public void MarkFailed(string errorCode, DateTimeOffset utcNow)
    {
        Attempts++;
        LastErrorCode = string.IsNullOrWhiteSpace(errorCode) ? "ops.messaging.handler_failed"
            : errorCode.Length > ErrorCodeMaxLength ? errorCode[..ErrorCodeMaxLength] : errorCode;
        LeasedUntilUtc = null;
        NextAttemptUtc = Attempts >= MaxAttempts ? null : utcNow.AddMinutes(Math.Min(60, Math.Pow(2, Attempts - 1)));
        MarkAsModified(utcNow);
    }
}
