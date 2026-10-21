using Vixly.Operations.Domain.Customers;

namespace Vixly.Operations.Application.Counter;

// =========================================================
// FILE: OperationsCounterCustomer.cs
//
// PURPOSE:
// The customer as the front-desk clerk sees them (Slice 5C, C-5).
//
// RESPONSIBILITIES:
// - Carry who they are and how to reach them
//
// IMPORTANT NOTES:
// - Contact details only: no credit, balances or notes (C-9)
// =========================================================
public sealed record OperationsCounterCustomer(Guid Id, string? Code, string Name, string? CompanyName, string Phone, string Email, OperationsCustomerStatus Status)
{
    public static OperationsCounterCustomer Of(OperationsCustomer customer)
    {
        ArgumentNullException.ThrowIfNull(customer);
        return new OperationsCounterCustomer(customer.Id, customer.Code, customer.FirstName + " " + customer.LastName, customer.CompanyName, customer.Phone, customer.Email.Value,
            customer.Status);
    }
}
