namespace Vixly.Contracts.Operations.Customers;

// =========================================================
// FILE: CustomerTokenBody.cs
//
// PURPOSE:
// A single-use link token (Slice 2).
//
// RESPONSIBILITIES:
// - Carry the values between Customer.Web and Operations.Api
//
// IMPORTANT NOTES:
// - Wire type: add fields at the end, never rename (ADR-0014)
// =========================================================
public sealed record CustomerTokenBody(string Token);
