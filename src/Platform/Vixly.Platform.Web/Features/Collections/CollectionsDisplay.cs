using System.Globalization;
using Microsoft.Extensions.Localization;
using Vixly.Platform.Application.Queries;
using Vixly.Platform.Domain.Collections;
using Vixly.Shared.Localization;
using Vixly.Shared.UI.Primitives;

namespace Vixly.Platform.Web.Features.Collections;

// =========================================================
// FILE: CollectionsDisplay.cs
//
// PURPOSE:
// Text keys, badges and formatting for the collections screens.
//
// RESPONSIBILITIES:
// - Map stages, statuses and close reasons to text keys and badges
// - Write a history line from its code and arguments
// - Format amounts per currency
//
// IMPORTANT NOTES:
// - Display only; amounts are never added across currencies
// =========================================================
public static class CollectionsDisplay
{
    public static string StageKey(PlatformCollectionStage stage)
    {
        return "ui.collections.stage." + Billing.BillingDisplay.Snake(stage.ToString());
    }

    public static VxBadgeStatus StageBadge(PlatformCollectionStage stage)
    {
        return stage switch
        {
            PlatformCollectionStage.Blocked => VxBadgeStatus.Danger,
            PlatformCollectionStage.FinalWarning => VxBadgeStatus.Warning,
            PlatformCollectionStage.Opened => VxBadgeStatus.Info,
            _ => VxBadgeStatus.Suspended
        };
    }

    public static string StatusKey(PlatformCollectionCaseStatus status)
    {
        return "ui.collections.case_status." + Billing.BillingDisplay.Snake(status.ToString());
    }

    public static string CloseReasonKey(PlatformCollectionCloseReason reason)
    {
        return "ui.collections.close_reason." + Billing.BillingDisplay.Snake(reason.ToString());
    }

    public static string AgeingKey(int bucket)
    {
        return "ui.collections.ageing." + bucket.ToString(CultureInfo.InvariantCulture);
    }

    public static string Amounts(IReadOnlyList<PlatformMoneyItem> amounts)
    {
        return amounts.Count == 0
            ? "-"
            : string.Join(", ", amounts.Select(item => Billing.BillingDisplay.Money(item.Amount, item.CurrencyCode)));
    }

    public static string Date(DateTimeOffset? value)
    {
        return value?.UtcDateTime.ToString("d", CultureInfo.CurrentCulture) ?? "-";
    }

    public static string Date(DateTime? value)
    {
        return value?.ToString("d", CultureInfo.CurrentCulture) ?? "-";
    }

    // "platform.collection_history.blocked" -> "ui.collection_history.blocked", with dates in the reader's culture.
    public static string History(IStringLocalizer text, string eventCode, IReadOnlyDictionary<string, string> arguments)
    {
        ArgumentNullException.ThrowIfNull(text);
        ArgumentNullException.ThrowIfNull(arguments);

        Dictionary<string, object> values = new(StringComparer.Ordinal);

        foreach ((string key, string value) in arguments)
        {
            values[key] = key is "blockDate" or "due" or "through" && DateTimeOffset.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out DateTimeOffset parsed)
                ? parsed.UtcDateTime.ToString("d", CultureInfo.CurrentCulture)
                : value;
        }

        return VixlyText.Format(text, "ui.collection_history." + eventCode[(eventCode.LastIndexOf('.') + 1)..], values);
    }
}
