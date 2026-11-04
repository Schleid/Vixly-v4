namespace Vixly.Operations.Application.UseCases.Counter.SaveOperationsCounterSignature;

// =========================================================
// FILE: SaveOperationsCounterSignatureRequest.cs
//
// PURPOSE:
// The customer signs on screen at the counter for their declaration and insurance choice (Slice 5D, D-8).
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class SaveOperationsCounterSignatureRequest
{
    public Guid ShipmentId { get; init; }

    // A PNG image of the signature, as drawn.
    public byte[]? Image { get; init; }
}
