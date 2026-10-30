namespace Vixly.Operations.Domain.Insurance;

// =========================================================
// FILE: OperationsPolicyRisks.cs
//
// PURPOSE:
// The risks a policy covers (Slice 5B).
//
// RESPONSIBILITIES:
// - List the usual cargo and liability risks
//
// IMPORTANT NOTES:
// - Flags: a policy covers several
// =========================================================
[Flags]
public enum OperationsPolicyRisks
{
    None = 0,

    Loss = 1,

    Damage = 2,

    Theft = 4,

    Wetting = 8,

    Contamination = 16,

    WarAndStrikes = 32,

    GeneralAverage = 64
}
