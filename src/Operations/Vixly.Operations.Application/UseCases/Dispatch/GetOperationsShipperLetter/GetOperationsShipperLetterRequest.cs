namespace Vixly.Operations.Application.UseCases.Dispatch.GetOperationsShipperLetter;

// =========================================================
// FILE: GetOperationsShipperLetterRequest.cs
//
// PURPOSE:
// The Shipper's Letter of Instruction of a dispatch as a PDF (Slice 6B step 2, D-6; as V3).
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class GetOperationsShipperLetterRequest
{
    public Guid DispatchId { get; init; }
}
