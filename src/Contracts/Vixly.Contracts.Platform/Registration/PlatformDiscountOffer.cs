namespace Vixly.Contracts.Platform.Registration;

// =========================================================
// FILE: PlatformDiscountOffer.cs
//
// PURPOSE:
// A discount as shown to a prospect at registration.
//
// RESPONSIBILITIES:
// - Carry data over HTTP between Public.Web and Platform.Api
//
// IMPORTANT NOTES:
// - Kind is percent or fixed_amount; Duration is periods, until_date or no_end
// - Wire type: BCL only (C-0); never a domain type
// =========================================================
public sealed record PlatformDiscountOffer(
    string Kind,
    decimal Value,
    string Duration,
    int? Periods,
    DateTimeOffset? EndsUtc);
