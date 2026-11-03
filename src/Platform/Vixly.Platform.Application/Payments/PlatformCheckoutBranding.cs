using Vixly.Platform.Domain.Payments;

namespace Vixly.Platform.Application.Payments;

// =========================================================
// FILE: PlatformCheckoutBranding.cs
//
// PURPOSE:
// How the hosted checkout page looks: name, logo, icon and colours.
//
// RESPONSIBILITIES:
// - Carry the look set in Control > Payments settings to the provider adapter
// - Build itself from the merchant account
//
// IMPORTANT NOTES:
// - Every field is optional; empty fields keep the provider dashboard's branding
// - Colours are "#RRGGBB"; image addresses are absolute https
// =========================================================
public sealed record PlatformCheckoutBranding(
    string? DisplayName,
    string? LogoUrl,
    string? IconUrl,
    string? BackgroundColor,
    string? ButtonColor)
{
    public bool IsEmpty => DisplayName is null && LogoUrl is null && IconUrl is null && BackgroundColor is null && ButtonColor is null;

    public static PlatformCheckoutBranding? From(PlatformMerchantAccount? account)
    {
        if (account is null)
        {
            return null;
        }

        PlatformCheckoutBranding branding = new(account.CheckoutDisplayName, account.CheckoutLogoUrl, account.CheckoutIconUrl, account.CheckoutBackgroundColor, account.CheckoutButtonColor);
        return branding.IsEmpty ? null : branding;
    }
}
