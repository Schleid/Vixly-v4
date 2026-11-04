namespace Vixly.Operations.Application.UseCases.Bundles.GetOperationsRepackBoxLabel;

// =========================================================
// FILE: GetOperationsRepackBoxLabelRequest.cs
//
// PURPOSE:
// The 4 x 6 label of a repack box (Slice 6A, D-4).
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class GetOperationsRepackBoxLabelRequest
{
    public Guid BoxId { get; init; }
}
