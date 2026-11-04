namespace Vixly.Operations.Application.UseCases.Customers.CreateOperationsCustomer;

// =========================================================
// FILE: CreateOperationsCustomerRequest.cs
//
// PURPOSE:
// Lets staff enter a customer when the company's plan has no customer portal (Vixly Lite, owner 2026-10-30).
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class CreateOperationsCustomerRequest
{
    public string FirstName { get; init; } = string.Empty;

    public string LastName { get; init; } = string.Empty;

    public string? CompanyName { get; init; }

    public string Email { get; init; } = string.Empty;

    public string Phone { get; init; } = string.Empty;

    public string Language { get; init; } = "en";
}
