namespace Vixly.Contracts.Operations.Customers;

// =========================================================
// FILE: CustomerPasswordChangeBody.cs
//
// PURPOSE:
// The signed-in customer's password change (Slice 2).
//
// RESPONSIBILITIES:
// - Carry the values between Customer.Web and Operations.Api
//
// IMPORTANT NOTES:
// - Wire type: add fields at the end, never rename (ADR-0014)
// =========================================================
public sealed record CustomerPasswordChangeBody(string CurrentPassword, string NewPassword);
