namespace Vixly.Contracts.Platform.Registration;

// =========================================================
// FILE: PlatformPromotionCheck.cs
//
// PURPOSE:
// A valid promotion code at the plan step (D-S20).
//
// RESPONSIBILITIES:
// - Carry data over HTTP between Public.Web and Platform.Api
//
// IMPORTANT NOTES:
// - Wire type: BCL only (C-0); never a domain type
// =========================================================
public sealed record PlatformPromotionCheck(string Name, PlatformDiscountOffer Discount);
