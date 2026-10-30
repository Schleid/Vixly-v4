namespace Vixly.Operations.Application.UseCases.Receiving.FindOperationsParcelByScan;

// =========================================================
// FILE: FindOperationsParcelByScanRequest.cs
//
// PURPOSE:
// Finds a parcel from a scan at a station: its ID label, or the carrier barcode (Slice 3B).
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class FindOperationsParcelByScanRequest
{
    public string Barcode { get; init; } = string.Empty;
}
