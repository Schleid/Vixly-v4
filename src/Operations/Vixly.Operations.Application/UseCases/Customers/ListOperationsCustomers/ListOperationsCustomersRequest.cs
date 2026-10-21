namespace Vixly.Operations.Application.UseCases.Customers.ListOperationsCustomers;

// =========================================================
// FILE: ListOperationsCustomersRequest.cs
//
// PURPOSE:
// Staff list the customers: search by name, email, phone or code, 25 per page (Slice 2).
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class ListOperationsCustomersRequest
{
    public string? Search { get; init; }

    public int Page { get; init; } = 1;
}
