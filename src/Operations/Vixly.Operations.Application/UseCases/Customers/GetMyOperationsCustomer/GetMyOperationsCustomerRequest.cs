namespace Vixly.Operations.Application.UseCases.Customers.GetMyOperationsCustomer;

// =========================================================
// FILE: GetMyOperationsCustomerRequest.cs
//
// PURPOSE:
// The signed-in customer's profile and mailbox addresses (Slice 2).
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class GetMyOperationsCustomerRequest
{
    public string SessionToken { get; init; } = string.Empty;
}
