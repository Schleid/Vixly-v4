namespace Vixly.Contracts.Operations.Customers;

// =========================================================
// FILE: CustomerSessionDto.cs
//
// PURPOSE:
// A new session and the customer it belongs to (Slice 2).
//
// RESPONSIBILITIES:
// - Carry the values between Customer.Web and Operations.Api
//
// IMPORTANT NOTES:
// - Wire type: add fields at the end, never rename (ADR-0014)
// =========================================================
public sealed record CustomerSessionDto(string SessionToken, CustomerProfileDto Customer);
