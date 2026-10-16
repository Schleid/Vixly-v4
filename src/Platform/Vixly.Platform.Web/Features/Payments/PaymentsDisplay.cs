using Vixly.Platform.Domain.Payments;
using Vixly.Shared.UI.Primitives;

namespace Vixly.Platform.Web.Features.Payments;

// =========================================================
// FILE: PaymentsDisplay.cs
//
// PURPOSE:
// Text keys and badges for the card-payment screens.
//
// RESPONSIBILITIES:
// - Map card transaction states, origins, providers and modes to text keys and badges
//
// IMPORTANT NOTES:
// - Display only
// =========================================================
public static class PaymentsDisplay
{
    public static string StatusKey(PlatformCardTransactionStatus status)
    {
        return "ui.card_payments.status." + Billing.BillingDisplay.Snake(status.ToString());
    }

    public static VxBadgeStatus Badge(PlatformCardTransactionStatus status)
    {
        return status switch
        {
            PlatformCardTransactionStatus.Succeeded => VxBadgeStatus.Success,
            PlatformCardTransactionStatus.Failed => VxBadgeStatus.Danger,
            PlatformCardTransactionStatus.RequiresAction => VxBadgeStatus.Warning,
            PlatformCardTransactionStatus.Cancelled => VxBadgeStatus.Deactivated,
            _ => VxBadgeStatus.Info
        };
    }

    public static string OriginKey(PlatformCardTransactionOrigin origin)
    {
        return "ui.card_payments.origin." + Billing.BillingDisplay.Snake(origin.ToString());
    }

    public static string KindKey(PlatformCardTransactionKind kind)
    {
        return "ui.card_payments.kind." + Billing.BillingDisplay.Snake(kind.ToString());
    }

    public static string DisputeKey(PlatformDisputeStatus status)
    {
        return "ui.card_payments.dispute." + Billing.BillingDisplay.Snake(status.ToString());
    }

    public static string ProviderKey(PlatformCardProvider provider)
    {
        return "ui.payments_settings.provider." + Billing.BillingDisplay.Snake(provider.ToString());
    }

    public static string ModeKey(PlatformPaymentMode mode)
    {
        return "ui.payments_settings.mode." + Billing.BillingDisplay.Snake(mode.ToString());
    }

    public static string Card(string brand, string last4)
    {
        return brand.ToUpperInvariant() + " \u2022\u2022\u2022\u2022 " + last4;
    }
}
