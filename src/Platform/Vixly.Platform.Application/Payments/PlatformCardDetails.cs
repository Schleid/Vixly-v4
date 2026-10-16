namespace Vixly.Platform.Application.Payments;

// =========================================================
// FILE: PlatformCardDetails.cs
//
// PURPOSE:
// What Vixly may keep of a card: reference, brand, last four and expiry.
//
// RESPONSIBILITIES:
// - Carry what Vixly may keep of a card: reference, brand, last four and expiry
//
// IMPORTANT NOTES:
// - Never a card number or CVV (D-P2)
// =========================================================
public sealed record PlatformCardDetails(string PaymentMethodId, string Brand, string Last4, int ExpiryMonth, int ExpiryYear);
