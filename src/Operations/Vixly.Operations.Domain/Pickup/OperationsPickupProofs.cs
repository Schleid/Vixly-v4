namespace Vixly.Operations.Domain.Pickup;

// =========================================================
// FILE: OperationsPickupProofs.cs
//
// PURPOSE:
// The proof a company asks at its pickup counter (Slice 7 step 2, R-5).
//
// RESPONSIBILITIES:
// - Name each proof as a flag
//
// IMPORTANT NOTES:
// - Stored as a number; never renumber
// - An ID and a signature by default; a code and a photo when the company switches them on
// =========================================================
[Flags]
public enum OperationsPickupProofs
{
    None = 0,
    Id = 1,
    Signature = 2,
    Code = 4,
    Photo = 8,
    Default = Id | Signature,
    All = Id | Signature | Code | Photo
}
