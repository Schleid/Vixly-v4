namespace Vixly.Contracts.Platform.Payments;

// =========================================================
// FILE: PlatformCheckoutBody.cs
//
// PURPOSE:
// The tenant's choices before going to the card provider's page.
//
// RESPONSIBILITIES:
// - Carry data over HTTP between Public.Web and Platform.Api
//
// IMPORTANT NOTES:
// - No amount: the server computes it (D-P7)
// - AutoCharge means the tenant accepted the consent text of ConsentTextVersion
// - Wire type: BCL only (C-0); never a domain type
// =========================================================
public sealed record PlatformCheckoutBody(bool SaveCard, bool AutoCharge);
