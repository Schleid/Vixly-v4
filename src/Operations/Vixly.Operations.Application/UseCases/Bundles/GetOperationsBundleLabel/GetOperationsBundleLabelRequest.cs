namespace Vixly.Operations.Application.UseCases.Bundles.GetOperationsBundleLabel;

// =========================================================
// FILE: GetOperationsBundleLabelRequest.cs
//
// PURPOSE:
// The 4 x 6 label of a bundle (Slice 6A, D-2; owner, A-6).
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class GetOperationsBundleLabelRequest
{
    public Guid BundleId { get; init; }
}
