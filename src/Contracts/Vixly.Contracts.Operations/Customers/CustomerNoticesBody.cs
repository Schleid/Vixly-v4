namespace Vixly.Contracts.Operations.Customers;

// =========================================================
// FILE: CustomerNoticesBody.cs
//
// PURPOSE:
// The signed-in customer's choice of channel for the company's notices, with their consent to texts (Operations Slice 9).
//
// RESPONSIBILITIES:
// - Carry the values between Customer.Web and Operations.Api
//
// IMPORTANT NOTES:
// - Wire type: add fields at the end, never rename (ADR-0014)
// =========================================================
public sealed record CustomerNoticesBody(string? Channel, bool Consent);
