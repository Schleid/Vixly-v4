namespace Vixly.Operations.Application.UseCases.Counter.GetOperationsCounterDeclaration;

// =========================================================
// FILE: GetOperationsCounterDeclarationRequest.cs
//
// PURPOSE:
// The page the customer signs at the counter: their declaration of each parcel and the insurance they chose or declined (Slice 5D, D-8).
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class GetOperationsCounterDeclarationRequest
{
    public Guid ShipmentId { get; init; }
}
