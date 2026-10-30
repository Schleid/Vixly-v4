namespace Vixly.Operations.Application.UseCases.Dispatch.GetOperationsDispatch;

// =========================================================
// FILE: GetOperationsDispatchRequest.cs
//
// PURPOSE:
// One dispatch (Slice 6B, D-2 to D-4): its bundles, the bundles that fit it, its legs and the carriers.
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class GetOperationsDispatchRequest
{
    public Guid DispatchId { get; init; }
}
