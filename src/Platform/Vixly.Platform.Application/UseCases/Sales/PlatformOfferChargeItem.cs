namespace Vixly.Platform.Application.UseCases.Sales;

// =========================================================
// FILE: PlatformOfferChargeItem.cs
//
// PURPOSE:
// A one-time charge agreed in an offer.
//
// RESPONSIBILITIES:
// - Carry a description and an amount
//
// IMPORTANT NOTES:
// - Also the input shape when preparing an offer
// =========================================================
public sealed record PlatformOfferChargeItem(string Description, decimal Amount);
