namespace Vixly.Operations.Application.UseCases.Dispatch.AddBundleToOperationsDispatch;

// =========================================================
// FILE: AddBundleToOperationsDispatchRequest.cs
//
// PURPOSE:
// Put a closed bundle on a planned dispatch, chosen or scanned (Slice 6B, D-3).
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class AddBundleToOperationsDispatchRequest
{
    public Guid DispatchId { get; init; }

    public Guid? BundleId { get; init; }

    // Scanned or typed.
    public string? BundleNumber { get; init; }
}
