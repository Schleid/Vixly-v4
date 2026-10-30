namespace Vixly.Operations.Application.UseCases.Customers.SignUpOperationsCustomer;

// =========================================================
// FILE: SignUpOperationsCustomerRequest.cs
//
// PURPOSE:
// A customer signs up on the tenant portal (Slice 2).
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class SignUpOperationsCustomerRequest
{
    public string FirstName { get; init; } = string.Empty;

    public string LastName { get; init; } = string.Empty;

    public string? CompanyName { get; init; }

    public string Email { get; init; } = string.Empty;

    public string Phone { get; init; } = string.Empty;

    public string Language { get; init; } = "en";

    public string Password { get; init; } = string.Empty;

    public string PortalSlug { get; init; } = string.Empty;
}
