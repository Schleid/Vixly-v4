namespace Vixly.Contracts.Operations.Billing;

// =========================================================
// FILE: CustomerPayReportDto.cs
//
// PURPOSE:
// A payment the customer reported on the portal, waiting for the company (owner, 2026-10-27).
//
// RESPONSIBILITIES:
// - Carry the invoice and payment numbers
//
// IMPORTANT NOTES:
// - Finance confirms it in the bank (Payments to verify)
// =========================================================
public sealed record CustomerPayReportDto(string InvoiceNumber, string PaymentNumber);
