using System.Globalization;
using Microsoft.Extensions.Localization;
using Vixly.Platform.Application.Queries;
using Vixly.Platform.Application.UseCases.Sales;
using Vixly.Platform.Domain.Subscriptions;
using Vixly.Shared.Localization;
using Vixly.Shared.UI.Primitives;

namespace Vixly.Platform.Web.Features;

// =========================================================
// FILE: SubscriptionDisplay.cs
//
// PURPOSE:
// Text keys and formats for plans and subscriptions.
//
// RESPONSIBILITIES:
// - Map statuses, terms, features and limits to text keys and badges
// - Format money and the subscription history in the reader language
//
// IMPORTANT NOTES:
// - Money shows the plan currency code; amounts are before tax (D-S9)
// - No literal text: everything goes through PlatformText (UI-10)
// =========================================================
public static class SubscriptionDisplay
{
    public static string StatusKey(PlatformSubscriptionStatus status)
    {
        return "ui.subscriptions.status." + Snake(status.ToString());
    }

    public static VxBadgeStatus Badge(PlatformSubscriptionStatus status)
    {
        return status switch
        {
            PlatformSubscriptionStatus.Active => VxBadgeStatus.Active,
            PlatformSubscriptionStatus.Trialing => VxBadgeStatus.Invited,
            PlatformSubscriptionStatus.PendingPayment or PlatformSubscriptionStatus.PastDue => VxBadgeStatus.Suspended,
            PlatformSubscriptionStatus.Suspended => VxBadgeStatus.Locked,
            _ => VxBadgeStatus.Deactivated
        };
    }

    public static string PlanStatusKey(PlatformPlanStatus status)
    {
        return "ui.plans.status." + Snake(status.ToString());
    }

    public static VxBadgeStatus PlanBadge(PlatformPlanStatus status)
    {
        return status == PlatformPlanStatus.Active ? VxBadgeStatus.Active : VxBadgeStatus.Deactivated;
    }

    public static string VersionStatusKey(PlatformPlanVersionStatus status)
    {
        return "ui.plans.version_status." + Snake(status.ToString());
    }

    public static string TermKey(PlatformSubscriptionTerm term)
    {
        return term == PlatformSubscriptionTerm.Annual ? "ui.subscriptions.term.annual" : "ui.subscriptions.term.monthly";
    }

    public static string FeatureKey(PlatformPlanFeature feature)
    {
        return "ui.plans.feature." + Snake(feature.ToString());
    }

    public static string LimitKey(PlatformPlanLimitKind kind)
    {
        return "ui.plans.limit." + Snake(kind.ToString());
    }

    public static string AccessKey(PlatformSubscriptionAccess access)
    {
        return "ui.subscriptions.access." + Snake(access.ToString());
    }

    public static string Money(decimal? amount, string currencyCode)
    {
        return amount is null ? "-" : amount.Value.ToString("N2", CultureInfo.CurrentCulture) + " " + currencyCode;
    }

    public static string Date(DateTimeOffset? value)
    {
        return value is null ? "-" : value.Value.UtcDateTime.ToString("d", CultureInfo.CurrentCulture);
    }

    public static string History(IStringLocalizer text, PlatformSubscriptionEventItem item)
    {
        ArgumentNullException.ThrowIfNull(text);
        ArgumentNullException.ThrowIfNull(item);

        Dictionary<string, object> values = new(StringComparer.Ordinal);

        foreach ((string key, string value) in item.Arguments)
        {
            values[key] = key switch
            {
                "from" or "to" when item.EventCode == PlatformSubscriptionEventCodes.SetupFeeChanged && decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out decimal fee) => fee.ToString("N2", CultureInfo.CurrentCulture),
                "endReason" when item.EventCode == PlatformSubscriptionEventCodes.DiscountEnded && Enum.TryParse(value, out PlatformDiscountEndReason ended) => text["ui.discounts.end_reason." + Snake(ended.ToString())].Value,
                "source" when Enum.TryParse(value, out PlatformDiscountSource source) => text[Sales.SalesDisplay.SourceKey(source)].Value,
                "term" or "fromTerm" when Enum.TryParse(value, out PlatformSubscriptionTerm term) => text[TermKey(term)].Value,
                "endReason" when Enum.TryParse(value, out PlatformSubscriptionEndReason reason) => text["ui.subscriptions.end_reason." + Snake(reason.ToString())].Value,
                "trialEnds" or "from" or "to" or "periodEnd" or "effective" when DateTimeOffset.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out DateTimeOffset date) => Date(date),
                "charge" or "amount" when decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out decimal amount) => amount.ToString("N2", CultureInfo.CurrentCulture),
                _ => value
            };
        }

        if (DiscountTerms(item.Arguments) is { } terms)
        {
            values["terms"] = Sales.SalesDisplay.Describe(text, terms, item.Arguments.GetValueOrDefault("currency"));
        }

        return VixlyText.Format(text, item.EventCode.Replace("platform.", "ui.", StringComparison.Ordinal), values);
    }

    // Discount events record their terms as separate arguments; the history shows them as one phrase.
    private static PlatformDiscountTermsItem? DiscountTerms(IReadOnlyDictionary<string, string> arguments)
    {
        if (!arguments.TryGetValue("kind", out string? kindText)
            || !Enum.TryParse(kindText, out PlatformDiscountKind kind)
            || !arguments.TryGetValue("value", out string? valueText)
            || !decimal.TryParse(valueText, NumberStyles.Number, CultureInfo.InvariantCulture, out decimal value)
            || !Enum.TryParse(arguments.GetValueOrDefault("duration"), out PlatformDiscountDuration duration))
        {
            return null;
        }

        int? periods = int.TryParse(arguments.GetValueOrDefault("periods"), NumberStyles.Integer, CultureInfo.InvariantCulture, out int count) ? count : null;
        DateTimeOffset? ends = DateTimeOffset.TryParse(arguments.GetValueOrDefault("ends"), CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out DateTimeOffset date) ? date : null;

        return new PlatformDiscountTermsItem(kind, value, duration, periods, ends);
    }

    private static string Snake(string value)
    {
        return string.Concat(value.Select((character, index) =>
            char.IsUpper(character) && index > 0 ? "_" + char.ToLowerInvariant(character) : char.ToLowerInvariant(character).ToString()));
    }
}
