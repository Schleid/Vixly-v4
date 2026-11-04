using Vixly.Operations.Domain.Common;

namespace Vixly.Operations.Domain.Texts;

// =========================================================
// FILE: OperationsTextMessage.cs
//
// PURPOSE:
// One text message sent (or tried) to a customer (Slice 9), for the company’s log.
//
// RESPONSIBILITIES:
// - Keep the customer, the channel, the notice, the number’s last digits, what Twilio answered
//
// IMPORTANT NOTES:
// - Never the text itself or the whole number
// =========================================================
public sealed class OperationsTextMessage : OperationsEntity
{
    private OperationsTextMessage()
    {
        ToHint = string.Empty;
    }

    private OperationsTextMessage(Guid tenantId, DateTimeOffset utcNow)
        : base(tenantId, utcNow)
    {
        ToHint = string.Empty;
    }

    public Guid CustomerId { get; private set; }

    public OperationsNoticeChannel Channel { get; private set; }

    public OperationsTextNotices Notice { get; private set; }

    public string ToHint { get; private set; }

    public OperationsTextStatus Status { get; private set; }

    public string? ProviderId { get; private set; }

    public string? ErrorCode { get; private set; }

    // What Twilio reported later (its status callback): only a message it accepted moves on, and a delivered one stays delivered.
    public bool Report(OperationsTextStatus status, string? errorCode, DateTimeOffset utcNow)
    {
        if (Status is not (OperationsTextStatus.Sent or OperationsTextStatus.Delivered) || status is not (OperationsTextStatus.Delivered or OperationsTextStatus.Undelivered)
            || (Status == OperationsTextStatus.Delivered && status == OperationsTextStatus.Undelivered) || Status == status)
        {
            return false;
        }

        Status = status;
        ErrorCode = errorCode is { Length: > 32 } ? errorCode[..32] : errorCode;
        MarkAsModified(utcNow);
        return true;
    }

    public static OperationsTextMessage Record(Guid tenantId, Guid customerId, OperationsNoticeChannel channel, OperationsTextNotices notice, string to, OperationsTextStatus status,
        string? providerId, string? errorCode, DateTimeOffset utcNow) =>
        new(tenantId, utcNow)
        {
            CustomerId = customerId,
            Channel = channel,
            Notice = notice,
            ToHint = to.Length > 4 ? "…" + to[^4..] : to,
            Status = status,
            ProviderId = providerId is { Length: > 64 } ? providerId[..64] : providerId,
            ErrorCode = errorCode is { Length: > 32 } ? errorCode[..32] : errorCode
        };
}
