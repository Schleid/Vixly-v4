namespace Vixly.Contracts.Platform.Registration;

// =========================================================
// FILE: PlatformOfferChargeLine.cs
//
// PURPOSE:
// A one-time charge agreed in an offer link.
//
// RESPONSIBILITIES:
// - Carry data over HTTP between Public.Web and Platform.Api
//
// IMPORTANT NOTES:
// - Amount before tax, in the plan currency
// - Wire type: BCL only (C-0); never a domain type
// =========================================================
public sealed record PlatformOfferChargeLine(string Description, decimal Amount);
