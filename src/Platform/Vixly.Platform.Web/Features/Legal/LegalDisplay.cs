using Microsoft.Extensions.Localization;
using Vixly.Platform.Domain.Legal;
using Vixly.Shared.UI.Primitives;

namespace Vixly.Platform.Web.Features.Legal;

// =========================================================
// FILE: LegalDisplay.cs
//
// PURPOSE:
// Text keys and badges for the legal screens.
//
// RESPONSIBILITIES:
// - Map kinds, document statuses, legal statuses, sources and reminder steps to text keys
//
// IMPORTANT NOTES:
// - Display only
// =========================================================
public static class LegalDisplay
{
    public static string KindKey(PlatformLegalDocumentKind kind) => "ui.legal.kind." + Billing.BillingDisplay.Snake(kind.ToString());

    public static string DocumentStatusKey(PlatformLegalDocumentStatus status) => "ui.legal.document_status." + Billing.BillingDisplay.Snake(status.ToString());

    public static string StatusKey(PlatformLegalStatus status) => "ui.legal.status." + Billing.BillingDisplay.Snake(status.ToString());

    public static string SourceKey(PlatformTermsAcceptanceSource source) => "ui.legal.source." + Billing.BillingDisplay.Snake(source.ToString());

    public static string StepKey(PlatformTermsReminderStep step) => "ui.legal.step." + Billing.BillingDisplay.Snake(step.ToString());

    public static string LanguageKey(string language) => "ui.legal.language." + language;

    public static VxBadgeStatus DocumentBadge(PlatformLegalDocumentStatus status) => status switch
    {
        PlatformLegalDocumentStatus.Published => VxBadgeStatus.Active,
        PlatformLegalDocumentStatus.Draft => VxBadgeStatus.Info,
        _ => VxBadgeStatus.Deactivated
    };

    public static VxBadgeStatus Badge(PlatformLegalStatus status) => status switch
    {
        PlatformLegalStatus.Accepted => VxBadgeStatus.Success,
        PlatformLegalStatus.Due => VxBadgeStatus.Warning,
        PlatformLegalStatus.Overdue => VxBadgeStatus.Danger,
        _ => VxBadgeStatus.Deactivated
    };

    public static string Date(DateTimeOffset? value) => value?.UtcDateTime.ToString("d", System.Globalization.CultureInfo.CurrentCulture) ?? "-";

    public static string Size(int bytes) => (bytes / 1024.0).ToString("N0", System.Globalization.CultureInfo.CurrentCulture) + " KB";
}
