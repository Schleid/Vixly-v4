namespace Vixly.Contracts.Operations.Customers;

// =========================================================
// FILE: CustomerSignInBody.cs
//
// PURPOSE:
// A sign-in with email and password (Slice 2).
//
// RESPONSIBILITIES:
// - Carry the values between Customer.Web and Operations.Api
//
// IMPORTANT NOTES:
// - Wire type: add fields at the end, never rename (ADR-0014)
// =========================================================
public sealed record CustomerSignInBody(string Email, string Password);
