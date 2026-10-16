namespace Vixly.Platform.Application.Payments;

// =========================================================
// FILE: PlatformCheckoutPurpose.cs
//
// PURPOSE:
// Whether a hosted checkout takes a payment or only saves a card.
//
// RESPONSIBILITIES:
// - Carry whether a hosted checkout takes a payment or only saves a card
//
// IMPORTANT NOTES:
// - Plain data; no behaviour
// =========================================================
public enum PlatformCheckoutPurpose
{
    Payment = 1,
    SaveCard = 2
}
