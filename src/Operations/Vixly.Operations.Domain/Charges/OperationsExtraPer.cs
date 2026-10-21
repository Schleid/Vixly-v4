namespace Vixly.Operations.Domain.Charges;

// =========================================================
// FILE: OperationsExtraPer.cs
//
// PURPOSE:
// What an extra charge counts (Slice 4 design section 4).
//
// RESPONSIBILITIES:
// - Name the choices
//
// IMPORTANT NOTES:
// - Stored as numbers; never renumber
// =========================================================
public enum OperationsExtraPer
{
    Item = 1,
    Piece = 2,
    Shipment = 3
}
