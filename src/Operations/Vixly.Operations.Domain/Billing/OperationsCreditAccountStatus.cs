namespace Vixly.Operations.Domain.Billing;

// =========================================================
// FILE: OperationsCreditAccountStatus.cs
//
// PURPOSE:
// Where a customer's credit account stands (Slice 5F step 2b, K-1).
//
// RESPONSIBILITIES:
// - Name the states
//
// IMPORTANT NOTES:
// - Stored as numbers; never renumber
// =========================================================
public enum OperationsCreditAccountStatus
{
    Active = 1,
    OnHold = 2,
    Closed = 3
}
