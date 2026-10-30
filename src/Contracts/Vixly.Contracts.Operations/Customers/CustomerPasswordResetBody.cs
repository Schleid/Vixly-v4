namespace Vixly.Contracts.Operations.Customers;

// =========================================================
// FILE: CustomerPasswordResetBody.cs
//
// PURPOSE:
// A new password from a reset or invitation link (Slice 2).
//
// RESPONSIBILITIES:
// - Carry the values between Customer.Web and Operations.Api
//
// IMPORTANT NOTES:
// - Wire type: add fields at the end, never rename (ADR-0014)
// =========================================================
public sealed record CustomerPasswordResetBody(string Token, string Password);
