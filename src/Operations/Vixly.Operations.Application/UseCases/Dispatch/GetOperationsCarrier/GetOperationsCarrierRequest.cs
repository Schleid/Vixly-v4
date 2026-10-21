namespace Vixly.Operations.Application.UseCases.Dispatch.GetOperationsCarrier;

// =========================================================
// FILE: GetOperationsCarrierRequest.cs
//
// PURPOSE:
// One carrier with its rates (Slice 6B, D-1).
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class GetOperationsCarrierRequest
{
    public Guid CarrierId { get; init; }
}
