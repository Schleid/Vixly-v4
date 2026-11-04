namespace Vixly.Operations.Application.Charges;

// =========================================================
// FILE: OperationsRatingPiece.cs
//
// PURPOSE:
// One piece to price: its weight, size, declared value, item type and contents (Slice 4 and 4B).
//
// RESPONSIBILITIES:
// - Carry a parcel or a typed piece into the rating engine
//
// IMPORTANT NOTES:
// - Weight and size in kilograms and centimetres (D-O8); value in the company's currency
// - ItemTypeId: one of the company's item types (Slice 4B section 7)
// =========================================================
public sealed record OperationsRatingPiece(decimal WeightKg, decimal LengthCm, decimal WidthCm, decimal HeightCm, decimal DeclaredValue, Guid? ItemTypeId = null, string? Contents = null);
