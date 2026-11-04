namespace Vixly.Operations.Domain.Charges;

// =========================================================
// FILE: OperationsInsuredRisks.cs
//
// PURPOSE:
// The risks an insurance covers (Slice 4C section 8).
//
// RESPONSIBILITIES:
// - Name the risks; a plan covers one or more
//
// IMPORTANT NOTES:
// - Stored as numbers; never renumber
// =========================================================
[Flags]
public enum OperationsInsuredRisks
{
    None = 0,

    Loss = 1,

    Damage = 2,

    Theft = 4
}
