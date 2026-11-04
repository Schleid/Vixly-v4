namespace Vixly.Operations.Application.UseCases.Bundles.ScanIntoOperationsRepackBox;

// =========================================================
// FILE: ScanIntoOperationsRepackBoxRequest.cs
//
// PURPOSE:
// A worker scans a parcel of the shipment into its open repack box (Slice 6A, D-4).
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class ScanIntoOperationsRepackBoxRequest
{
    public Guid BoxId { get; init; }

    public string? Code { get; init; }

    public Guid ScanKey { get; init; }
}
