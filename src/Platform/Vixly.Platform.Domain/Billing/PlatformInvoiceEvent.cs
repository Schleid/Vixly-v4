using System.Globalization;
using Vixly.Shared.Kernel.Domain;

namespace Vixly.Platform.Domain.Billing;

// =========================================================
// FILE: PlatformInvoiceEvent.cs
//
// PURPOSE:
// One entry in an invoice's history.
//
// RESPONSIBILITIES:
// - Record what happened, who did it, why, and the values to show
//
// IMPORTANT NOTES:
// - Append-only: no update or delete (DENY in the global script)
// - Arguments are rendered in the reader's language by the console
// =========================================================
public sealed class PlatformInvoiceEvent : AuditableEntity
{
    private PlatformInvoiceEvent()
    {
        EventCode = string.Empty;
        Arguments = new Dictionary<string, string>(StringComparer.Ordinal);
    }

    private PlatformInvoiceEvent(Guid invoiceId, string eventCode, Guid? actorUserId, string? reason, IReadOnlyDictionary<string, string> arguments, DateTimeOffset utcNow)
        : base(utcNow)
    {
        PlatformInvoiceId = invoiceId;
        EventCode = eventCode;
        ActorUserId = actorUserId;
        Reason = reason;
        Arguments = arguments;
    }

    public Guid PlatformInvoiceId { get; private set; }

    public string EventCode { get; private set; }

    public Guid? ActorUserId { get; private set; }

    public string? Reason { get; private set; }

    public IReadOnlyDictionary<string, string> Arguments { get; private set; }

    public static PlatformInvoiceEvent Record(Guid invoiceId, string eventCode, Guid? actorUserId, DateTimeOffset utcNow, string? reason = null, IReadOnlyDictionary<string, string>? arguments = null)
    {
        return new PlatformInvoiceEvent(invoiceId, eventCode, actorUserId, reason, arguments ?? new Dictionary<string, string>(StringComparer.Ordinal), utcNow);
    }

    public static string Amount(decimal value)
    {
        return value.ToString("0.00", CultureInfo.InvariantCulture);
    }
}
