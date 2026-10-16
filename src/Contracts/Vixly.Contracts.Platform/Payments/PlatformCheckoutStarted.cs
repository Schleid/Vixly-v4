namespace Vixly.Contracts.Platform.Payments;

// =========================================================
// FILE: PlatformCheckoutStarted.cs
//
// PURPOSE:
// Where to send the browser: the card provider's hosted page.
//
// RESPONSIBILITIES:
// - Carry data over HTTP between Public.Web and Platform.Api
//
// IMPORTANT NOTES:
// - Wire type: BCL only (C-0); never a domain type
// =========================================================
public sealed record PlatformCheckoutStarted(string CheckoutUrl);
