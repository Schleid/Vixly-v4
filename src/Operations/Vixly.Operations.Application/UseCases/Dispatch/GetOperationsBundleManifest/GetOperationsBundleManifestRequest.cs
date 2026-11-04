namespace Vixly.Operations.Application.UseCases.Dispatch.GetOperationsBundleManifest;

// =========================================================
// FILE: GetOperationsBundleManifestRequest.cs
//
// PURPOSE:
// A bundle's manifest as a PDF (Slice 6B, D-6): the shipments and parcels inside it.
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class GetOperationsBundleManifestRequest
{
    public Guid BundleId { get; init; }
}
