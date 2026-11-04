namespace Vixly.Operations.Domain.Screens;

// =========================================================
// FILE: OperationsScreenRecordKind.cs
//
// PURPOSE:
// What a customer did on a customer screen that the company keeps (Slice 5F step 3, P-3, P-9).
//
// RESPONSIBILITIES:
// - Name each kind
//
// IMPORTANT NOTES:
// - Stored as numbers; never renumber
// =========================================================
public enum OperationsScreenRecordKind
{
    // The customer accepted the company's Zelle terms for an invoice.
    ZelleTerms = 1
}
