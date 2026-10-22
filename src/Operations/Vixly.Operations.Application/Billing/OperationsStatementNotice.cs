using Vixly.Operations.Application.Ports;

namespace Vixly.Operations.Application.Billing;

// =========================================================
// FILE: OperationsStatementNotice.cs
//
// PURPOSE:
// The outbox message that tells a customer their monthly statement is ready (Slice 5F step 2b, K-7).
//
// RESPONSIBILITIES:
// - Carry the customer and the portal address
//
// IMPORTANT NOTES:
// - Sent by OperationsStatementNoticeHandler after the transaction commits
// =========================================================
public sealed record OperationsStatementNotice(Guid CustomerId, string PortalSlug)
{
    public const string MessageType = "ops.credit.statement.v1";

    public static OperationsCustomerMailKind Kind => OperationsCustomerMailKind.CreditStatement;
}
