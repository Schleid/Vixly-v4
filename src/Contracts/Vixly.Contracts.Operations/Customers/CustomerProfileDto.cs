namespace Vixly.Contracts.Operations.Customers;

// =========================================================
// FILE: CustomerProfileDto.cs
//
// PURPOSE:
// The signed-in customer as the portal shows them (Slice 2).
//
// RESPONSIBILITIES:
// - Carry the values between Customer.Web and Operations.Api
//
// IMPORTANT NOTES:
// - Wire type: add fields at the end, never rename (ADR-0014)
// =========================================================
public sealed record CustomerProfileDto(Guid CustomerId, string? Code, string FirstName, string LastName, string? CompanyName, string Email, string Phone, string Language, IReadOnlyList<CustomerMailboxDto> Mailboxes);
