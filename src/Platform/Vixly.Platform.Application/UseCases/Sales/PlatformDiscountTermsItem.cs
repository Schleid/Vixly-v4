using Vixly.Platform.Domain.Subscriptions;

namespace Vixly.Platform.Application.UseCases.Sales;

// =========================================================
// FILE: PlatformDiscountTermsItem.cs
//
// PURPOSE:
// Discount terms for display.
//
// RESPONSIBILITIES:
// - Carry kind, value and duration
//
// IMPORTANT NOTES:
// - Built from PlatformDiscountTerms
// =========================================================
public sealed record PlatformDiscountTermsItem(
    PlatformDiscountKind Kind,
    decimal Value,
    PlatformDiscountDuration Duration,
    int? Periods,
    DateTimeOffset? EndsUtc)
{
    public static PlatformDiscountTermsItem From(PlatformDiscountTerms terms)
    {
        ArgumentNullException.ThrowIfNull(terms);

        return new PlatformDiscountTermsItem(terms.Kind, terms.Value, terms.Duration, terms.Periods, terms.EndsUtc);
    }
}
