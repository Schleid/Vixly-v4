namespace Vixly.Operations.Domain.Api;

// =========================================================
// FILE: OperationsWebhookEventNames.cs
//
// PURPOSE:
// The wire names of webhook events (Slice 10).
//
// RESPONSIBILITIES:
// - Name each event as the receiver reads it
//
// IMPORTANT NOTES:
// - Never change a name: receivers depend on it
// =========================================================
public static class OperationsWebhookEventNames
{
    public const string Tracking = "tracking.recorded";

    public const string CustomerCreated = "customer.created";

    public const string InvoiceIssued = "invoice.issued";

    public const string PaymentRecorded = "payment.recorded";

    public const string Test = "webhook.test";

    public static IReadOnlyList<OperationsWebhookEvents> All { get; } = new[]
    {
        OperationsWebhookEvents.Tracking, OperationsWebhookEvents.CustomerCreated, OperationsWebhookEvents.InvoiceIssued, OperationsWebhookEvents.PaymentRecorded
    };

    public static string Of(OperationsWebhookEvents kind) => kind switch
    {
        OperationsWebhookEvents.Tracking => Tracking,
        OperationsWebhookEvents.CustomerCreated => CustomerCreated,
        OperationsWebhookEvents.InvoiceIssued => InvoiceIssued,
        OperationsWebhookEvents.PaymentRecorded => PaymentRecorded,
        _ => throw new ArgumentOutOfRangeException(nameof(kind))
    };
}
