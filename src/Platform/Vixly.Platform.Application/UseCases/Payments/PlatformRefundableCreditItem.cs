namespace Vixly.Platform.Application.UseCases.Payments;

// =========================================================
// FILE: PlatformRefundableCreditItem.cs
//
// PURPOSE:
// An issued credit note that can still be refunded to the card.
//
// RESPONSIBILITIES:
// - Carry what the console or public page shows
//
// IMPORTANT NOTES:
// - Plain record; no card data beyond brand, last four and expiry
// =========================================================
public sealed record PlatformRefundableCreditItem(Guid CreditId, string Number, decimal Refundable);
