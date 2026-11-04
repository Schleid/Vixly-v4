namespace Vixly.Operations.Application.Dispatch;

// =========================================================
// FILE: OperationsManifestShipment.cs
//
// PURPOSE:
// One shipment on a manifest (Slice 6B, D-6).
//
// RESPONSIBILITIES:
// - Carry its number, customer, consignee, pieces, weight, value and contents
//
// IMPORTANT NOTES:
// - Weights in kilograms; the layout shows the company's unit
// =========================================================
public sealed record OperationsManifestShipment(string Number, string CustomerCode, string Consignee, string CountryCode, int Pieces, decimal WeightKg, decimal DeclaredValue, string CurrencyCode,
    string Contents);
