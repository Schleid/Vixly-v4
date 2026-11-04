namespace Vixly.Operations.Application.UseCases.Customers.GetOperationsCustomer;

// =========================================================
// FILE: GetOperationsCustomerRequest.cs
//
// PURPOSE:
// Staff open one customer, with the packages coming to them (Slice 2; owner, 2026-10-18).
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class GetOperationsCustomerRequest
{
    public Guid CustomerId { get; init; }
}
