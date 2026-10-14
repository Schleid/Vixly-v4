namespace Vixly.Contracts.Platform.Registration;

// =========================================================
// FILE: PlatformRegistrationOffer.cs
//
// PURPOSE:
// The agreed terms behind an offer link, shown before the prospect registers (D-S21).
//
// RESPONSIBILITIES:
// - Carry data over HTTP between Public.Web and Platform.Api
//
// IMPORTANT NOTES:
// - The email the link is tied to is masked
// - Term is monthly or annual; amounts before tax
// - Wire type: BCL only (C-0); never a domain type
// =========================================================
public sealed record PlatformRegistrationOffer(
    string CompanyName,
    string? MaskedEmail,
    string PlanCode,
    string PlanName,
    string CurrencyCode,
    string Term,
    int Licences,
    decimal PeriodAmount,
    PlatformDiscountOffer? Discount,
    decimal SetupFee,
    IReadOnlyList<PlatformOfferChargeLine> Charges,
    bool StartTrial,
    int? TrialDays,
    DateTimeOffset ExpiresUtc);
