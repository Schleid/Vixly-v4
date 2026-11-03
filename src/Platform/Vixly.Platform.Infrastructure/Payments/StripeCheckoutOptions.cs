using Stripe;
using Stripe.Checkout;
using Vixly.Platform.Application.Payments;

namespace Vixly.Platform.Infrastructure.Payments;

// =========================================================
// FILE: StripeCheckoutOptions.cs
//
// PURPOSE:
// Builds the Stripe request options for a hosted checkout and for an off-session charge.
//
// RESPONSIBILITIES:
// - Choose the payment methods each checkout may offer
// - Add the checkout page branding and the statement descriptor suffix
//
// IMPORTANT NOTES:
// - payment_method_types is never sent: the API refuses it on this account; methods are dynamic and
//   narrowed per payment with allowed_payment_method_types (not modelled in Stripe.net 52.4.2, so sent as an extra parameter)
// - A US bank account debit is offered only for payments, only when asked, and only in USD (provider rule)
// - Card saving and registration stay card only; off-session charges use the saved card only
// - Registration (CardOnly) also hides Link, whose bank option is not a card (owner, 2026-11-02); Apple Pay and Google Pay stay
// =========================================================
internal static class StripeCheckoutOptions
{
    public const string Card = "card";

    public const string UsBankAccount = "us_bank_account";

    public const string AllowedMethodsParameter = "allowed_payment_method_types";

    // Checkout's value for not offering Link at all.
    public const string LinkHidden = "never";

    // The only currency a US bank account debit can be taken in.
    private const string BankAccountCurrency = "usd";

    public static IReadOnlyList<string> AllowedMethods(PlatformCheckoutRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        return request.Purpose == PlatformCheckoutPurpose.Payment && request.AllowBankAccount && !request.SaveCard
            && string.Equals(request.CurrencyCode, BankAccountCurrency, StringComparison.OrdinalIgnoreCase)
            ? new[] { Card, UsBankAccount }
            : new[] { Card };
    }

    public static SessionCreateOptions Session(PlatformCheckoutRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        Dictionary<string, string> metadata = new(request.Metadata, StringComparer.Ordinal);
        SessionCreateOptions options = new()
        {
            Customer = request.CustomerId,
            SuccessUrl = request.SuccessUrl,
            CancelUrl = request.CancelUrl,
            Locale = request.Language is "en" or "fr" or "es" ? request.Language : "auto",
            Metadata = metadata,
            ClientReferenceId = metadata.GetValueOrDefault(PlatformCardMetadata.Transaction) ?? metadata.GetValueOrDefault(PlatformCardMetadata.Link),
            BrandingSettings = Branding(request.Branding)
        };
        options.AddExtraParam(AllowedMethodsParameter, AllowedMethods(request).ToList());

        if (request.CardOnly)
        {
            options.WalletOptions = new SessionWalletOptionsOptions { Link = new SessionWalletOptionsLinkOptions { Display = LinkHidden } };
        }

        if (request.Purpose == PlatformCheckoutPurpose.SaveCard)
        {
            options.Mode = "setup";
            options.SetupIntentData = new SessionSetupIntentDataOptions { Metadata = metadata };
            return options;
        }

        options.Mode = "payment";
        options.LineItems = new List<SessionLineItemOptions>
        {
            new()
            {
                Quantity = 1,
                PriceData = new SessionLineItemPriceDataOptions
                {
                    Currency = request.CurrencyCode.ToLowerInvariant(),
                    UnitAmount = request.AmountMinor,
                    ProductData = new SessionLineItemPriceDataProductDataOptions { Name = request.Description }
                }
            }
        };
        options.PaymentIntentData = new SessionPaymentIntentDataOptions
        {
            Description = request.Description,
            Metadata = metadata,
            SetupFutureUsage = request.SaveCard ? "off_session" : null,
            StatementDescriptorSuffix = Text(request.StatementSuffix)
        };
        return options;
    }

    public static PaymentIntentCreateOptions Charge(PlatformCardChargeRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        return new PaymentIntentCreateOptions
        {
            Amount = request.AmountMinor,
            Currency = request.CurrencyCode.ToLowerInvariant(),
            Customer = request.CustomerId,
            PaymentMethod = request.PaymentMethodId,
            AllowedPaymentMethodTypes = new List<string> { Card },
            OffSession = true,
            Confirm = true,
            Description = request.Description,
            Metadata = new Dictionary<string, string>(request.Metadata, StringComparer.Ordinal),
            StatementDescriptorSuffix = Text(request.StatementSuffix)
        };
    }

    private static SessionBrandingSettingsOptions? Branding(PlatformCheckoutBranding? branding)
    {
        if (branding is null || branding.IsEmpty)
        {
            return null;
        }

        return new SessionBrandingSettingsOptions
        {
            DisplayName = Text(branding.DisplayName),
            BackgroundColor = Text(branding.BackgroundColor),
            ButtonColor = Text(branding.ButtonColor),
            Logo = Text(branding.LogoUrl) is { } logo ? new SessionBrandingSettingsLogoOptions { Type = "url", Url = logo } : null,
            Icon = Text(branding.IconUrl) is { } icon ? new SessionBrandingSettingsIconOptions { Type = "url", Url = icon } : null
        };
    }

    private static string? Text(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
