using System.Globalization;
using Microsoft.Extensions.Localization;
using Vixly.Platform.Application.Queries;
using Vixly.Platform.Application.UseCases.Sales;
using Vixly.Platform.Domain.Sales;
using Vixly.Platform.Domain.Subscriptions;
using Vixly.Shared.Localization;
using Vixly.Shared.UI.Primitives;

namespace Vixly.Platform.Web.Features.Sales;

// =========================================================
// FILE: SalesDisplay.cs
//
// PURPOSE:
// Text keys and short descriptions for discounts, promotions, offers and approvals.
//
// RESPONSIBILITIES:
// - Map statuses to keys and badges
// - Describe discount terms in the reader's language
//
// IMPORTANT NOTES:
// - Keys are ui.discounts.*, ui.promotions.*, ui.offers.*, ui.approvals.*
// =========================================================
public static class SalesDisplay
{
    public static string Describe(IStringLocalizer text, PlatformDiscountTermsItem? terms, string? currencyCode)
    {
        ArgumentNullException.ThrowIfNull(text);

        if (terms is null)
        {
            return text["ui.discounts.none"].Value;
        }

        string value = terms.Kind == PlatformDiscountKind.Percent
            ? VixlyText.Format(text, "ui.discounts.percent_value", "value", terms.Value.ToString("0.##", CultureInfo.CurrentCulture))
            : VixlyText.Format(text, "ui.discounts.fixed_value", "amount", SubscriptionDisplay.Money(terms.Value, currencyCode ?? string.Empty));
        string duration = terms.Duration switch
        {
            PlatformDiscountDuration.Periods => VixlyText.Format(text, "ui.discounts.for_periods", "count", terms.Periods ?? 0),
            PlatformDiscountDuration.UntilDate => VixlyText.Format(text, "ui.discounts.until_date", "date", SubscriptionDisplay.Date(terms.EndsUtc)),
            _ => text["ui.discounts.no_end"].Value
        };

        return value + " " + duration;
    }

    public static string KindKey(PlatformDiscountKind kind)
    {
        return kind == PlatformDiscountKind.Percent ? "ui.discounts.kind.percent" : "ui.discounts.kind.fixed_amount";
    }

    public static string DurationKey(PlatformDiscountDuration duration)
    {
        return duration switch
        {
            PlatformDiscountDuration.Periods => "ui.discounts.duration.periods",
            PlatformDiscountDuration.UntilDate => "ui.discounts.duration.until_date",
            _ => "ui.discounts.duration.no_end"
        };
    }

    public static string SourceKey(PlatformDiscountSource source)
    {
        return source switch
        {
            PlatformDiscountSource.Promotion => "ui.discounts.source.promotion",
            PlatformDiscountSource.Offer => "ui.discounts.source.offer",
            _ => "ui.discounts.source.staff"
        };
    }

    public static string PromotionStatusKey(PlatformPromotionStatus status)
    {
        return status switch
        {
            PlatformPromotionStatus.PendingApproval => "ui.promotions.status.pending_approval",
            PlatformPromotionStatus.Active => "ui.promotions.status.active",
            PlatformPromotionStatus.Rejected => "ui.promotions.status.rejected",
            PlatformPromotionStatus.Paused => "ui.promotions.status.paused",
            PlatformPromotionStatus.Ended => "ui.promotions.status.ended",
            _ => "ui.promotions.status.draft"
        };
    }

    public static VxBadgeStatus PromotionBadge(PlatformPromotionStatus status)
    {
        return status switch
        {
            PlatformPromotionStatus.Active => VxBadgeStatus.Active,
            PlatformPromotionStatus.PendingApproval => VxBadgeStatus.Warning,
            PlatformPromotionStatus.Rejected => VxBadgeStatus.Danger,
            PlatformPromotionStatus.Paused => VxBadgeStatus.Suspended,
            PlatformPromotionStatus.Ended => VxBadgeStatus.Deactivated,
            _ => VxBadgeStatus.Info
        };
    }

    public static string OfferStatusKey(PlatformOfferStatus status, DateTimeOffset expiresUtc, DateTimeOffset utcNow)
    {
        if (status == PlatformOfferStatus.Issued && expiresUtc <= utcNow)
        {
            return "ui.offers.status.expired";
        }

        return status switch
        {
            PlatformOfferStatus.PendingApproval => "ui.offers.status.pending_approval",
            PlatformOfferStatus.Issued => "ui.offers.status.issued",
            PlatformOfferStatus.Rejected => "ui.offers.status.rejected",
            PlatformOfferStatus.Used => "ui.offers.status.used",
            PlatformOfferStatus.Revoked => "ui.offers.status.revoked",
            _ => "ui.offers.status.draft"
        };
    }

    public static VxBadgeStatus OfferBadge(PlatformOfferStatus status, DateTimeOffset expiresUtc, DateTimeOffset utcNow)
    {
        if (status == PlatformOfferStatus.Issued && expiresUtc <= utcNow)
        {
            return VxBadgeStatus.Deactivated;
        }

        return status switch
        {
            PlatformOfferStatus.Issued => VxBadgeStatus.Active,
            PlatformOfferStatus.PendingApproval => VxBadgeStatus.Warning,
            PlatformOfferStatus.Rejected or PlatformOfferStatus.Revoked => VxBadgeStatus.Danger,
            PlatformOfferStatus.Used => VxBadgeStatus.Success,
            _ => VxBadgeStatus.Info
        };
    }

    public static string ApprovalKindKey(PlatformApprovalKind kind)
    {
        return kind switch
        {
            PlatformApprovalKind.Discount => "ui.approvals.kind.discount",
            PlatformApprovalKind.Offer => "ui.approvals.kind.offer",
            PlatformApprovalKind.Promotion => "ui.approvals.kind.promotion",
            _ => "ui.approvals.kind.plan_version"
        };
    }

    public static string ApprovalLink(PlatformApprovalItem item)
    {
        ArgumentNullException.ThrowIfNull(item);

        return item.Kind switch
        {
            PlatformApprovalKind.Discount => "/tenants/" + item.PlatformTenantId + "#tenant-discount",
            PlatformApprovalKind.Offer => "/offers/" + item.TargetId,
            PlatformApprovalKind.Promotion => "/promotions/" + item.TargetId,
            _ => "/plans/" + item.TargetId + "#plan-approval"
        };
    }

    public static DateTimeOffset? StartOfDay(DateTime? date)
    {
        return date is DateTime day ? new DateTimeOffset(DateTime.SpecifyKind(day.Date, DateTimeKind.Utc)) : null;
    }

    public static DateTimeOffset? EndOfDay(DateTime? date)
    {
        return date is DateTime day ? new DateTimeOffset(DateTime.SpecifyKind(day.Date.AddDays(1).AddSeconds(-1), DateTimeKind.Utc)) : null;
    }
}
