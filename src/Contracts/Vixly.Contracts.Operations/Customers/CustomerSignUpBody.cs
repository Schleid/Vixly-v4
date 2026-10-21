namespace Vixly.Contracts.Operations.Customers;

// =========================================================
// FILE: CustomerSignUpBody.cs
//
// PURPOSE:
// A self sign-up (Slice 2).
//
// RESPONSIBILITIES:
// - Carry the values between Customer.Web and Operations.Api
//
// IMPORTANT NOTES:
// - Wire type: add fields at the end, never rename (ADR-0014)
// =========================================================
public sealed record CustomerSignUpBody(string FirstName, string LastName, string? CompanyName, string Email, string Phone, string Language, string Password, string PortalSlug);
