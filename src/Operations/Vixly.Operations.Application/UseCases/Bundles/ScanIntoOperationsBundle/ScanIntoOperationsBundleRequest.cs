namespace Vixly.Operations.Application.UseCases.Bundles.ScanIntoOperationsBundle;

// =========================================================
// FILE: ScanIntoOperationsBundleRequest.cs
//
// PURPOSE:
// A worker scans a parcel or a repack box into an open bundle (Slice 6A, D-3, D-4 and D-5).
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class ScanIntoOperationsBundleRequest
{
    public Guid BundleId { get; init; }

    public string? Code { get; init; }

    // The scan form's reference: the same scan sent twice is one scan (ADR-0019).
    public Guid ScanKey { get; init; }
}
