using Vixly.Platform.Application.UseCases.Sales;
using Vixly.Platform.Domain.Subscriptions;

namespace Vixly.Platform.Application.UseCases.Registration;

// =========================================================
// FILE: PlatformRegistrationOfferPreview.cs
//
// PURPOSE:
// The agreed terms an offer link shows to the prospect before they register (D-S21).
//
// RESPONSIBILITIES:
// - Carry the plan, term, licences, prices, discount, fees and trial
//
// IMPORTANT NOTES:
// - The email the link is tied to is masked; the internal reason never leaves the console
// - Amounts before tax
// =========================================================
public sealed record PlatformRegistrationOfferPreview(
    string CompanyName,
    string? MaskedEmail,
    string PlanCode,
    string PlanName,
    string CurrencyCode,
    PlatformSubscriptionTerm Term,
    int Licences,
    decimal PeriodAmount,
    PlatformDiscountTermsItem? Discount,
    decimal SetupFee,
    IReadOnlyList<PlatformOfferChargeItem> Charges,
    bool StartTrial,
    int? TrialDays,
    DateTimeOffset ExpiresUtc)
{
    public static string? Mask(string? email)
    {
        if (string.IsNullOrEmpty(email))
        {
            return null;
        }

        int at = email.IndexOf('@', StringComparison.Ordinal);

        return at <= 1 ? "*" + email[Math.Max(at, 0)..] : email[0] + new string('*', at - 1) + email[at..];
    }
}
