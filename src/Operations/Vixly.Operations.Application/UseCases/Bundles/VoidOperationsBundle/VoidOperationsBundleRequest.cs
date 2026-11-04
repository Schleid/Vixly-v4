namespace Vixly.Operations.Application.UseCases.Bundles.VoidOperationsBundle;

// =========================================================
// FILE: VoidOperationsBundleRequest.cs
//
// PURPOSE:
// Void a bundle with a reason (Slice 6A, D-8; owner, A-4): its parcels and boxes are ready to bundle again.
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class VoidOperationsBundleRequest
{
    public Guid BundleId { get; init; }

    public string? Reason { get; init; }
}
