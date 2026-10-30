namespace Vixly.Operations.Application.Charges;

// =========================================================
// FILE: OperationsEstimateParcel.cs
//
// PURPOSE:
// One parcel in the shipment estimate (Slice 5D): its label, its size and its cost.
//
// RESPONSIBILITIES:
// - Carry the parcel's number or label ("Parcel 2"), its weight and dimensions, the billed size and the amount
//
// IMPORTANT NOTES:
// - Part of OperationsEstimate; always an estimate
// =========================================================
public sealed record OperationsEstimateParcel(int Number, string? Label, decimal WeightKg, decimal LengthCm, decimal WidthCm, decimal HeightCm, decimal? Billable, decimal Amount);
