namespace Vixly.Operations.Domain.Charges;

// =========================================================
// FILE: OperationsClaimFiler.cs
//
// PURPOSE:
// Who files a claim: the company for the customer, or the customer (Slice 4C section 8).
//
// RESPONSIBILITIES:
// - Name the two
//
// IMPORTANT NOTES:
// - Stored as numbers; never renumber
// =========================================================
public enum OperationsClaimFiler
{
    Company = 1,

    Customer = 2
}
