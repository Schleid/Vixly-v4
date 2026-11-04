namespace Vixly.Operations.Domain.Billing;

// =========================================================
// FILE: OperationsTenderStatus.cs
//
// PURPOSE:
// Whether the money of one tender is in (Slice 5F, C-5 and C-6).
//
// RESPONSIBILITIES:
// - Name the states
//
// IMPORTANT NOTES:
// - Stored as numbers; never renumber
// =========================================================
public enum OperationsTenderStatus
{
    Settled = 1,

    // Zelle or a bank transfer the customer says is sent; finance checks the bank.
    AwaitingConfirmation = 2,

    NotReceived = 3
}
