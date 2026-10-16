using System.Globalization;
using Microsoft.Extensions.Localization;
using Vixly.Platform.Application.UseCases.Billing;
using Vixly.Platform.Domain.Billing;
using Vixly.Shared.Localization;
using Vixly.Shared.UI.Primitives;

namespace Vixly.Platform.Web.Features.Billing;

// =========================================================
// FILE: BillingDisplay.cs
//
// PURPOSE:
// Text keys, badges, amounts and links for the Billing screens.
//
// RESPONSIBILITIES:
// - Map invoice, payment and credit note states to text keys and badges
// - Render invoice lines and history from their keys and arguments
// - Build the download link of a stored document
//
// IMPORTANT NOTES:
// - Display only; the use cases decide what is allowed
// - Amounts in arguments are invariant text; they are shown in the reader's culture
// =========================================================
public static class BillingDisplay
{
    public static string StatusKey(PlatformInvoiceStatus status)
    {
        return "ui.invoices.status." + Snake(status.ToString());
    }

    public static VxBadgeStatus Badge(PlatformInvoiceStatus status, bool overdue)
    {
        return status switch
        {
            PlatformInvoiceStatus.Paid => VxBadgeStatus.Success,
            PlatformInvoiceStatus.Void => VxBadgeStatus.Deactivated,
            _ => overdue ? VxBadgeStatus.Danger : VxBadgeStatus.Info
        };
    }

    public static string KindKey(PlatformInvoiceKind kind)
    {
        return "ui.invoices.kind." + Snake(kind.ToString());
    }

    public static string MethodKey(PlatformPaymentMethod method)
    {
        return "ui.payments.method." + Snake(method.ToString());
    }

    public static IReadOnlyList<PlatformPaymentMethod> ManualMethods { get; } = Enum.GetValues<PlatformPaymentMethod>().Where(method => method != PlatformPaymentMethod.Card).ToArray();

    public static string PaymentStatusKey(PlatformPaymentStatus status)
    {
        return "ui.payments.status." + Snake(status.ToString());
    }

    public static string CreditStatusKey(PlatformCreditNoteStatus status)
    {
        return "ui.credit_notes.status." + Snake(status.ToString());
    }

    public static string DocumentLink(PlatformBillingDocumentKind kind, Guid ownerId)
    {
        return "/billing/documents/" + Snake(kind.ToString()) + "/" + ownerId.ToString("D", CultureInfo.InvariantCulture);
    }

    public static string Money(decimal amount, string currencyCode)
    {
        return amount.ToString("N2", CultureInfo.CurrentCulture) + " " + currencyCode;
    }

    public static string Rate(decimal rate)
    {
        return rate.ToString("0.####", CultureInfo.CurrentCulture) + " %";
    }

    public static string Line(IStringLocalizer text, PlatformInvoiceLineItem line)
    {
        ArgumentNullException.ThrowIfNull(text);
        ArgumentNullException.ThrowIfNull(line);

        Dictionary<string, object> values = new(StringComparer.Ordinal);

        foreach ((string key, string value) in line.Arguments)
        {
            values[key] = key switch
            {
                "term" when Enum.TryParse(value, out Vixly.Platform.Domain.Subscriptions.PlatformSubscriptionTerm term) => text[SubscriptionDisplay.TermKey(term)].Value,
                "from" or "to" when DateTime.TryParseExact(value, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime date) => date.ToString("d", CultureInfo.CurrentCulture),
                _ => value
            };
        }

        return VixlyText.Format(text, line.TextKey, values);
    }

    public static string History(IStringLocalizer text, PlatformInvoiceEventItem item)
    {
        ArgumentNullException.ThrowIfNull(text);
        ArgumentNullException.ThrowIfNull(item);

        Dictionary<string, object> values = new(StringComparer.Ordinal);

        foreach ((string key, string value) in item.Arguments)
        {
            values[key] = key switch
            {
                "amount" or "total" when decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out decimal amount) => amount.ToString("N2", CultureInfo.CurrentCulture),
                "method" when Enum.TryParse(value, out PlatformPaymentMethod method) => text[MethodKey(method)].Value,
                _ => value
            };
        }

        return VixlyText.Format(text, item.EventCode.Replace("platform.", "ui.", StringComparison.Ordinal), values);
    }

    public static string Snake(string value)
    {
        return string.Concat(value.Select((character, index) =>
            char.IsUpper(character) && index > 0 ? "_" + char.ToLowerInvariant(character) : char.ToLowerInvariant(character).ToString()));
    }
}
