namespace Vixly.Operations.Domain.Charges;

// =========================================================
// FILE: OperationsInsurancePayout.cs
//
// PURPOSE:
// What a claim pays on: the declared value, or the lower of the declared and the invoice value (Slice 4C section 8).
//
// RESPONSIBILITIES:
// - Name the bases
//
// IMPORTANT NOTES:
// - Stored as numbers; never renumber
// =========================================================
public enum OperationsInsurancePayout
{
    DeclaredValue = 1,

    LowerOfDeclaredAndInvoice = 2
}
