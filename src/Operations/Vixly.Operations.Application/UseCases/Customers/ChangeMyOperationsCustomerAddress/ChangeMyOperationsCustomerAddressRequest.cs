namespace Vixly.Operations.Application.UseCases.Customers.ChangeMyOperationsCustomerAddress;

// =========================================================
// FILE: ChangeMyOperationsCustomerAddressRequest.cs
//
// PURPOSE:
// The signed-in customer gives or changes their home address (owner, 2026-10-26).
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class ChangeMyOperationsCustomerAddressRequest
{
    public string SessionToken { get; init; } = string.Empty;

    public string? Line1 { get; init; }

    public string? Line2 { get; init; }

    public string? City { get; init; }

    public string? Region { get; init; }

    public string? PostalCode { get; init; }

    public string? CountryCode { get; init; }
}
