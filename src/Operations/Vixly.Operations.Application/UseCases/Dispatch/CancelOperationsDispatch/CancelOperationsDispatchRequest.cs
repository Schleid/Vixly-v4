namespace Vixly.Operations.Application.UseCases.Dispatch.CancelOperationsDispatch;

// =========================================================
// FILE: CancelOperationsDispatchRequest.cs
//
// PURPOSE:
// Cancel a dispatch that has not left, with a reason (Slice 6B, D-2).
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class CancelOperationsDispatchRequest
{
    public Guid DispatchId { get; init; }

    public string? Reason { get; init; }
}
