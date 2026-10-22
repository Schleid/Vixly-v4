namespace Vixly.Operations.Application.UseCases.Bundles.CloseOperationsRepackBox;

// =========================================================
// FILE: CloseOperationsRepackBoxRequest.cs
//
// PURPOSE:
// Close a repack box with its weight and size once every parcel is in (Slice 6A, D-4).
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class CloseOperationsRepackBoxRequest
{
    public Guid BoxId { get; init; }

    // In the company's units.
    public decimal Weight { get; init; }

    public decimal Length { get; init; }

    public decimal Width { get; init; }

    public decimal Height { get; init; }
}
