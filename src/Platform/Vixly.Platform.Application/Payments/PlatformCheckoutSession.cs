using Vixly.Platform.Domain.Payments;

namespace Vixly.Platform.Application.Payments;

// =========================================================
// FILE: PlatformCheckoutSession.cs
//
// PURPOSE:
// A hosted checkout page opened at the provider.
//
// RESPONSIBILITIES:
// - Carry a hosted checkout page opened at the provider
//
// IMPORTANT NOTES:
// - Plain data; no behaviour
// =========================================================
public sealed record PlatformCheckoutSession(string CheckoutId, string Url);
