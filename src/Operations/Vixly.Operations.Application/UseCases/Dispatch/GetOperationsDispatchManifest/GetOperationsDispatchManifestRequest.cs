namespace Vixly.Operations.Application.UseCases.Dispatch.GetOperationsDispatchManifest;

// =========================================================
// FILE: GetOperationsDispatchManifestRequest.cs
//
// PURPOSE:
// The dispatch manifest as a PDF (Slice 6B, D-6): legs, bundles, shipments and consignees, for the carrier and customs.
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class GetOperationsDispatchManifestRequest
{
    public Guid DispatchId { get; init; }
}
