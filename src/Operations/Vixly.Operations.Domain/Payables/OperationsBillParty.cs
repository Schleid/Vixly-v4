namespace Vixly.Operations.Domain.Payables;

// =========================================================
// FILE: OperationsBillParty.cs
//
// PURPOSE:
// Who sent a bill (Slice 8): a carrier, a partner agent, or another vendor.
//
// RESPONSIBILITIES:
// - Name each kind
//
// IMPORTANT NOTES:
// - Stored as numbers; never renumber
// =========================================================
public enum OperationsBillParty
{
    Carrier = 1,
    Partner = 2,
    Vendor = 3
}
