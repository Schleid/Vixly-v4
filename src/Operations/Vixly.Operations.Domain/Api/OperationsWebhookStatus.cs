namespace Vixly.Operations.Domain.Api;

// =========================================================
// FILE: OperationsWebhookStatus.cs
//
// PURPOSE:
// Where a webhook delivery stands (Slice 10).
//
// RESPONSIBILITIES:
// - Name each state
//
// IMPORTANT NOTES:
// - Stored as a number; never renumber
// =========================================================
public enum OperationsWebhookStatus
{
    // Waiting for its first try or its next one.
    Pending = 1,

    // The receiver answered 2xx.
    Delivered = 2,

    // Every try failed; it can be sent again by hand.
    Failed = 3
}
