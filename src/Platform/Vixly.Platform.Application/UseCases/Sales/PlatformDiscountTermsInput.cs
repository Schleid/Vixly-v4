using Vixly.Platform.Domain.Subscriptions;
using Vixly.Shared.Kernel.Domain;

namespace Vixly.Platform.Application.UseCases.Sales;

// =========================================================
// FILE: PlatformDiscountTermsInput.cs
//
// PURPOSE:
// Discount terms as typed in the console or the API.
//
// RESPONSIBILITIES:
// - Carry kind, value and duration
// - Turn them into checked domain terms
//
// IMPORTANT NOTES:
// - Checking happens in PlatformDiscountTerms; a bad value raises its error code
// =========================================================
public sealed record PlatformDiscountTermsInput(
    PlatformDiscountKind Kind,
    decimal Value,
    PlatformDiscountDuration Duration,
    int? Periods,
    DateTimeOffset? EndsUtc)
{
    public PlatformDiscountTerms ToTerms(DateTimeOffset utcNow)
    {
        return PlatformDiscountTerms.Create(Kind, Value, Duration, Periods, EndsUtc, utcNow);
    }
}
