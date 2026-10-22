namespace Vixly.Operations.Application.Charges;

// =========================================================
// FILE: OperationsPieceInput.cs
//
// PURPOSE:
// One piece as a person types it, in the company's units (Slice 4 and 4B).
//
// RESPONSIBILITIES:
// - Carry the typed weight, size, value, item type and contents
//
// IMPORTANT NOTES:
// - Checked and converted by OperationsRatingSetup.Piece
// =========================================================
public sealed record OperationsPieceInput(decimal? Weight, decimal? Length, decimal? Width, decimal? Height, decimal? Value, Guid? ItemTypeId = null, string? Contents = null);
