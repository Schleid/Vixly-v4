namespace Vixly.Platform.Application.Collections;

// =========================================================
// FILE: PlatformCollectionsMailInvoice.cs
//
// PURPOSE:
// One overdue invoice in a collections email.
//
// RESPONSIBILITIES:
// - Carry number, due date, balance and the pay link, if any
//
// IMPORTANT NOTES:
// - The pay link is saved before the email is sent
// =========================================================
public sealed record PlatformCollectionsMailInvoice(string Number, DateTimeOffset DueUtc, decimal Balance, string CurrencyCode, string? PayLink);
