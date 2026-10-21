namespace Vixly.Contracts.Operations.Customers;

// =========================================================
// FILE: CustomerProfileBody.cs
//
// PURPOSE:
// The signed-in customer's profile change (Slice 2).
//
// RESPONSIBILITIES:
// - Carry the values between Customer.Web and Operations.Api
//
// IMPORTANT NOTES:
// - Wire type: add fields at the end, never rename (ADR-0014)
// =========================================================
public sealed record CustomerProfileBody(string FirstName, string LastName, string? CompanyName, string Phone, string Language);
