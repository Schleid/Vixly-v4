using System.Globalization;
using Vixly.Shared.Kernel.Domain;

namespace Vixly.Platform.Domain.Subscriptions;

// =========================================================
// FILE: PlatformDiscountTerms.cs
//
// PURPOSE:
// What a discount takes off and for how long (D-S18, D-S19).
//
// RESPONSIBILITIES:
// - Check the kind, value and duration
// - Work out the discount on a period amount or on a prorated charge
// - Describe itself for history and messages
//
// IMPORTANT NOTES:
// - Value object: used by subscription discounts, promotions and offer links
// - A fixed amount is in the subscription's currency and never takes a price below zero
// - No commercial limit lives here: what needs approval is a Sales setting (D-S22)
// =========================================================
public sealed class PlatformDiscountTerms
{
    public const int MaxPeriods = 120;

    public const int ChargeDescriptionMaxLength = 200;

    private PlatformDiscountTerms()
    {
    }

    private PlatformDiscountTerms(PlatformDiscountKind kind, decimal value, PlatformDiscountDuration duration, int? periods, DateTimeOffset? endsUtc)
    {
        Kind = kind;
        Value = value;
        Duration = duration;
        Periods = periods;
        EndsUtc = endsUtc;
    }

    public PlatformDiscountKind Kind { get; private set; }

    public decimal Value { get; private set; }

    public PlatformDiscountDuration Duration { get; private set; }

    public int? Periods { get; private set; }

    public DateTimeOffset? EndsUtc { get; private set; }

    public bool HasNoEnd => Duration == PlatformDiscountDuration.NoEnd;

    // =====================================================
    // METHOD: CREATE
    // =====================================================
    public static PlatformDiscountTerms Create(
        PlatformDiscountKind kind,
        decimal value,
        PlatformDiscountDuration duration,
        int? periods,
        DateTimeOffset? endsUtc,
        DateTimeOffset utcNow)
    {
        if (!Enum.IsDefined(kind))
        {
            throw new DomainException(PlatformDiscountErrors.KindInvalid);
        }

        if (decimal.Round(value, PlatformSubscriptionLimits.MoneyScale) != value)
        {
            throw new DomainException(kind == PlatformDiscountKind.Percent ? PlatformDiscountErrors.PercentInvalid : PlatformDiscountErrors.AmountInvalid);
        }

        if (kind == PlatformDiscountKind.Percent && (value <= 0m || value > 100m))
        {
            throw new DomainException(PlatformDiscountErrors.PercentInvalid);
        }

        if (kind == PlatformDiscountKind.FixedAmount && (value <= 0m || value > PlatformSubscriptionLimits.MaxPrice))
        {
            throw new DomainException(PlatformDiscountErrors.AmountInvalid);
        }

        switch (duration)
        {
            case PlatformDiscountDuration.Periods:
                if (periods is not (>= 1 and <= MaxPeriods))
                {
                    throw new DomainException(PlatformDiscountErrors.PeriodsInvalid);
                }

                return new PlatformDiscountTerms(kind, value, duration, periods, null);
            case PlatformDiscountDuration.UntilDate:
                if (endsUtc is not DateTimeOffset ends || ends <= utcNow || ends.Offset != TimeSpan.Zero)
                {
                    throw new DomainException(PlatformDiscountErrors.EndDateInvalid);
                }

                return new PlatformDiscountTerms(kind, value, duration, null, ends);
            case PlatformDiscountDuration.NoEnd:
                return new PlatformDiscountTerms(kind, value, duration, null, null);
            default:
                throw new DomainException(PlatformDiscountErrors.DurationInvalid);
        }
    }

    // =====================================================
    // METHOD: AMOUNTS
    // =====================================================
    public decimal Off(decimal periodAmount)
    {
        decimal off = Kind == PlatformDiscountKind.Percent
            ? PlatformPricing.Money(periodAmount * Value / 100m)
            : Value;

        return Math.Clamp(off, 0m, Math.Max(0m, periodAmount));
    }

    public decimal Net(decimal periodAmount)
    {
        return periodAmount - Off(periodAmount);
    }

    // A prorated charge (upgrade, added licences) is reduced by a
    // percentage; a fixed amount is already taken off the period.
    public decimal ReduceCharge(decimal charge)
    {
        return Kind == PlatformDiscountKind.Percent
            ? PlatformPricing.Money(charge * (100m - Value) / 100m)
            : charge;
    }

    public Dictionary<string, string> Describe()
    {
        Dictionary<string, string> arguments = new(StringComparer.Ordinal)
        {
            ["kind"] = Kind.ToString(),
            ["value"] = Value.ToString(CultureInfo.InvariantCulture),
            ["duration"] = Duration.ToString()
        };

        if (Periods is int count)
        {
            arguments["periods"] = count.ToString(CultureInfo.InvariantCulture);
        }

        if (EndsUtc is DateTimeOffset ends)
        {
            arguments["ends"] = ends.ToString("O", CultureInfo.InvariantCulture);
        }

        return arguments;
    }
}
