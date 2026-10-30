namespace Vixly.Operations.Application.UseCases.Dispatch.CloseOperationsDispatch;

// =========================================================
// FILE: CloseOperationsDispatchRequest.cs
//
// PURPOSE:
// Close (manifest) a dispatch with its bundles and legs (Slice 6B, D-2 and D-5).
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class CloseOperationsDispatchRequest
{
    public Guid DispatchId { get; init; }
}
