using Vixly.Platform.Application.UseCases.Sales;
using Vixly.Platform.Domain.Subscriptions;

namespace Vixly.Platform.Web.Features.Sales;

// =========================================================
// FILE: DiscountTermsModel.cs
//
// PURPOSE:
// The discount fields of a console form (D-S18, D-S19).
//
// RESPONSIBILITIES:
// - Hold kind, value and duration as typed
// - Turn them into use-case input
//
// IMPORTANT NOTES:
// - A date means the discount applies to periods starting before that day (UTC)
// =========================================================
public sealed class DiscountTermsModel
{
    public PlatformDiscountKind Kind { get; set; } = PlatformDiscountKind.Percent;

    public decimal Value { get; set; }

    public PlatformDiscountDuration Duration { get; set; } = PlatformDiscountDuration.Periods;

    public int? Periods { get; set; } = 1;

    public DateTime? EndsOn { get; set; }

    public PlatformDiscountTermsInput ToInput()
    {
        return new PlatformDiscountTermsInput(
            Kind,
            Value,
            Duration,
            Duration == PlatformDiscountDuration.Periods ? Periods : null,
            Duration == PlatformDiscountDuration.UntilDate && EndsOn is DateTime date ? new DateTimeOffset(DateTime.SpecifyKind(date.Date, DateTimeKind.Utc)) : null);
    }

    public static DiscountTermsModel From(PlatformDiscountTermsItem? item)
    {
        return item is null
            ? new DiscountTermsModel()
            : new DiscountTermsModel
            {
                Kind = item.Kind,
                Value = Math.Round(item.Value, 2),
                Duration = item.Duration,
                Periods = item.Periods,
                EndsOn = item.EndsUtc?.UtcDateTime.Date
            };
    }
}
