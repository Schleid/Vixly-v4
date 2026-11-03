namespace Vixly.Contracts.Operations.Customers;

// =========================================================
// FILE: CustomerNoticesDto.cs
//
// PURPOSE:
// How the signed-in customer gets the company's notices, and the channels the company offers (Operations Slice 9).
//
// RESPONSIBILITIES:
// - Carry the values between Operations.Api and Customer.Web
//
// IMPORTANT NOTES:
// - Wire type: add fields at the end, never rename (ADR-0014); channels as "email", "sms", "whatsapp"
// =========================================================
public sealed record CustomerNoticesDto(string Channel, IReadOnlyList<string> Offered, string Phone);
