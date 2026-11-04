namespace Vixly.Operations.Application.UseCases.Bundles.CloseOperationsBundle;

// =========================================================
// FILE: CloseOperationsBundleRequest.cs
//
// PURPOSE:
// Close a bundle with its weight and size (Slice 6A, D-7).
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class CloseOperationsBundleRequest
{
    public Guid BundleId { get; init; }

    // In the company's units.
    public decimal Weight { get; init; }

    public decimal Length { get; init; }

    public decimal Width { get; init; }

    public decimal Height { get; init; }
}
