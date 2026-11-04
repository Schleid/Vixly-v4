namespace Vixly.Operations.Domain.Delivery;

// =========================================================
// FILE: OperationsDeliveryProofs.cs
//
// PURPOSE:
// The proof taken at the door (Slice 7 step 3, R-15).
//
// RESPONSIBILITIES:
// - Name the values
//
// IMPORTANT NOTES:
// - Stored as a number; never renumber. The receiver's name is always asked
// =========================================================
[Flags]
public enum OperationsDeliveryProofs
{
    None = 0,
    Signature = 1,
    Photo = 2,
    Id = 4,
    Code = 8,
    Default = Signature,
    All = Signature | Photo | Id | Code
}
