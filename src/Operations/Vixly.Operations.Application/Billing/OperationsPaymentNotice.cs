using Vixly.Operations.Application.Ports;

namespace Vixly.Operations.Application.Billing;

// =========================================================
// FILE: OperationsPaymentNotice.cs
//
// PURPOSE:
// Tells a customer their Zelle or transfer did not arrive (Slice 5F, C-6).
//
// RESPONSIBILITIES:
// - Name the message and the customer email it becomes
//
// IMPORTANT NOTES:
// - Sent by OperationsPaymentNoticeHandler after the transaction commits
// =========================================================
public sealed record OperationsPaymentNotice(Guid CustomerId, Guid InvoiceId, string PortalSlug)
{
    public const string MessageType = "ops.payments.not_received.v1";

    public static OperationsCustomerMailKind Kind => OperationsCustomerMailKind.PaymentNotReceived;
}
