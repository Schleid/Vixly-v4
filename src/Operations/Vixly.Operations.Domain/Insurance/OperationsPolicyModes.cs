namespace Vixly.Operations.Domain.Insurance;

// =========================================================
// FILE: OperationsPolicyModes.cs
//
// PURPOSE:
// The transport modes a policy covers (Slice 5B).
//
// RESPONSIBILITIES:
// - Air, ocean, ground, in any mix
//
// IMPORTANT NOTES:
// - Flags; none means every mode
// =========================================================
[Flags]
public enum OperationsPolicyModes
{
    None = 0,

    Air = 1,

    Ocean = 2,

    Ground = 4
}
