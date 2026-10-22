namespace Vixly.Operations.Application.UseCases.Customers.UpdateMyOperationsCustomer;

// =========================================================
// FILE: UpdateMyOperationsCustomerRequest.cs
//
// PURPOSE:
// The signed-in customer changes their name, company, phone or language (Slice 2).
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class UpdateMyOperationsCustomerRequest
{
    public string SessionToken { get; init; } = string.Empty;

    public string FirstName { get; init; } = string.Empty;

    public string LastName { get; init; } = string.Empty;

    public string? CompanyName { get; init; }

    public string Phone { get; init; } = string.Empty;

    public string Language { get; init; } = "en";
}
