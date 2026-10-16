namespace Vixly.Platform.Application.UseCases.Payments;

// =========================================================
// FILE: PlatformZelleMail.cs
//
// PURPOSE:
// What a Zelle email says.
//
// RESPONSIBILITIES:
// - Carry the invoice, amount, send date and any reason
//
// IMPORTANT NOTES:
// - The reason is the one staff wrote for the customer
// =========================================================
public sealed record PlatformZelleMail(PlatformZelleNoticeKind Kind, string InvoiceNumber, decimal Amount, string CurrencyCode, DateTime SentOn, string? Reason);
