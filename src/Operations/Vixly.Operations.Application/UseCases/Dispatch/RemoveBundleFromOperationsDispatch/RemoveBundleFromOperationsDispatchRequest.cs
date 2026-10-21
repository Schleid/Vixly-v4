namespace Vixly.Operations.Application.UseCases.Dispatch.RemoveBundleFromOperationsDispatch;

// =========================================================
// FILE: RemoveBundleFromOperationsDispatchRequest.cs
//
// PURPOSE:
// Take a bundle off a planned dispatch (Slice 6B, D-3).
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class RemoveBundleFromOperationsDispatchRequest
{
    public Guid DispatchId { get; init; }

    public Guid BundleId { get; init; }
}
