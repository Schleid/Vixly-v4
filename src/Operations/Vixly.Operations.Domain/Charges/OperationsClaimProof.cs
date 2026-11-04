namespace Vixly.Operations.Domain.Charges;

// =========================================================
// FILE: OperationsClaimProof.cs
//
// PURPOSE:
// The proof a claim needs (Slice 4C section 8).
//
// RESPONSIBILITIES:
// - Name the proofs; a plan asks for one or more
//
// IMPORTANT NOTES:
// - Stored as numbers; never renumber
// =========================================================
[Flags]
public enum OperationsClaimProof
{
    None = 0,

    Invoice = 1,

    Photos = 2,

    // The damage was noted when the package was released.
    NotedAtRelease = 4
}
