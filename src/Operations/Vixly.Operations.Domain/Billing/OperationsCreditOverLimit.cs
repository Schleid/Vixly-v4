namespace Vixly.Operations.Domain.Billing;

// =========================================================
// FILE: OperationsCreditOverLimit.cs
//
// PURPOSE:
// What happens when a charge would go past the account's limit (Slice 5F step 2b, K-2; owner, 2026-10-20).
//
// RESPONSIBILITIES:
// - Name the company's choices
//
// IMPORTANT NOTES:
// - Stored as numbers; never renumber
// =========================================================
public enum OperationsCreditOverLimit
{
    Refuse = 1,
    AskCreditDepartment = 2
}
