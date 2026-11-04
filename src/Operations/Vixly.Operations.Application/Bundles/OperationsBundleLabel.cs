namespace Vixly.Operations.Application.Bundles;

// =========================================================
// FILE: OperationsBundleLabel.cs
//
// PURPOSE:
// What a bundle's 4 x 6 label shows (Slice 6A, D-2; owner, A-6).
//
// RESPONSIBILITIES:
// - Carry its number, route, service, type and, once closed, its weight
//
// IMPORTANT NOTES:
// - Weight and size in the company's units, already written
// =========================================================
public sealed record OperationsBundleLabel(string CompanyName, string Number, string OriginCode, string DestinationCode, string ServiceCode, string? TypeName, int Parcels, string? Weight,
    string? Size, bool Closed);
