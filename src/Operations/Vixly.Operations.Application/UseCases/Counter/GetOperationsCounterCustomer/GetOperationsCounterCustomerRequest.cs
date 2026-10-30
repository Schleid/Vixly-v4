namespace Vixly.Operations.Application.UseCases.Counter.GetOperationsCounterCustomer;

// =========================================================
// FILE: GetOperationsCounterCustomerRequest.cs
//
// PURPOSE:
// Shows a customer at the counter: who they are, all their shipments, and what a new shipment can be (Slice 5C, C-5 and C-6).
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class GetOperationsCounterCustomerRequest
{
    public Guid CustomerId { get; init; }
}
