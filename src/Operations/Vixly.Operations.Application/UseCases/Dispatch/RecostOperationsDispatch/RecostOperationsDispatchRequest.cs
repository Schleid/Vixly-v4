namespace Vixly.Operations.Application.UseCases.Dispatch.RecostOperationsDispatch;

// =========================================================
// FILE: RecostOperationsDispatchRequest.cs
//
// PURPOSE:
// Work out a dispatch's carrier cost again from the rates in force (Slice 6B step 2, D-7): after a rate was added, or a leg's carrier changed.
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class RecostOperationsDispatchRequest
{
    public Guid DispatchId { get; init; }
}
