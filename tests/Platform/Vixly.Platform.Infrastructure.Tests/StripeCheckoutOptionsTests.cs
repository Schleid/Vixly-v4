using Stripe;
using Stripe.Checkout;
using Vixly.Platform.Application.Payments;
using Vixly.Platform.Infrastructure.Payments;

namespace Vixly.Platform.Infrastructure.Tests;

// =========================================================
// FILE: StripeCheckoutOptionsTests.cs
//
// PURPOSE:
// Proves what Vixly asks Stripe for on the shared TechCrafters account (2026-11-02).
//
// RESPONSIBILITIES:
// - Never payment_method_types; methods narrowed per payment with allowed_payment_method_types
// - Registration and card saving: card only; other payments in USD: card and US bank account
// - Branding and the statement descriptor suffix; off-session charges keep the saved card
// - The async checkout events map to processing, settled and failed
//
// IMPORTANT NOTES:
// - No network: options and events are built in memory
// =========================================================
public sealed class StripeCheckoutOptionsTests
{
    private static PlatformCheckoutRequest Request(
        PlatformCheckoutPurpose purpose = PlatformCheckoutPurpose.Payment,
        bool saveCard = false,
        bool allowBank = true,
        string currency = "USD",
        PlatformCheckoutBranding? branding = null,
        string? suffix = null,
        bool cardOnly = false)
    {
        return new PlatformCheckoutRequest(purpose, "cus_1", 15000, currency, "INV-2026-000001", saveCard,
            new Dictionary<string, string> { [PlatformCardMetadata.Transaction] = "t-1" }, "https://vixly.test/ok", "https://vixly.test/no", "fr", "vx4-1",
            allowBank, branding, suffix, cardOnly);
    }

    private static List<string> Allowed(SessionCreateOptions options)
    {
        return Assert.IsType<List<string>>(options.ExtraParams[StripeCheckoutOptions.AllowedMethodsParameter]);
    }

    [Fact]
    public void Payment_InUsd_OffersCardAndUsBank_NeverPaymentMethodTypes()
    {
        SessionCreateOptions options = StripeCheckoutOptions.Session(Request());

        Assert.Null(options.PaymentMethodTypes);
        Assert.Equal(new[] { "card", "us_bank_account" }, Allowed(options));
        Assert.Equal("payment", options.Mode);
        Assert.Equal("fr", options.Locale);
        Assert.Equal("t-1", options.ClientReferenceId);
    }

    [Theory]
    [InlineData(false, false, "USD")]
    [InlineData(true, true, "USD")]
    [InlineData(true, false, "EUR")]
    public void CardOnly_ForRegistrationCardSavingOrAnotherCurrency(bool allowBank, bool saveCard, string currency)
    {
        Assert.Equal(new[] { "card" }, Allowed(StripeCheckoutOptions.Session(Request(allowBank: allowBank, saveCard: saveCard, currency: currency))));
    }

    [Fact]
    public void Registration_HidesLink_OtherCheckoutsKeepTheDefault()
    {
        SessionCreateOptions registration = StripeCheckoutOptions.Session(Request(allowBank: false, saveCard: true, cardOnly: true));
        SessionCreateOptions payLink = StripeCheckoutOptions.Session(Request());

        Assert.Equal("never", registration.WalletOptions!.Link!.Display);
        Assert.Equal(new[] { "card" }, Allowed(registration));
        Assert.Null(payLink.WalletOptions);
    }

    [Fact]
    public void SaveCard_IsSetupMode_CardOnly()
    {
        SessionCreateOptions options = StripeCheckoutOptions.Session(Request(PlatformCheckoutPurpose.SaveCard));

        Assert.Equal("setup", options.Mode);
        Assert.Equal(new[] { "card" }, Allowed(options));
        Assert.Null(options.PaymentIntentData);
    }

    [Fact]
    public void Branding_AndSuffix_AreSent_EmptyMeansStripeDashboard()
    {
        SessionCreateOptions branded = StripeCheckoutOptions.Session(Request(
            branding: new PlatformCheckoutBranding("Vixly by TechCrafters", "https://vixly.ltd/logo.png", null, "#0F172A", "#2563EB"), suffix: "VIXLY"));
        SessionCreateOptions plain = StripeCheckoutOptions.Session(Request());

        Assert.Equal("Vixly by TechCrafters", branded.BrandingSettings!.DisplayName);
        Assert.Equal("url", branded.BrandingSettings.Logo!.Type);
        Assert.Equal("https://vixly.ltd/logo.png", branded.BrandingSettings.Logo.Url);
        Assert.Null(branded.BrandingSettings.Icon);
        Assert.Equal("#0F172A", branded.BrandingSettings.BackgroundColor);
        Assert.Equal("#2563EB", branded.BrandingSettings.ButtonColor);
        Assert.Equal("VIXLY", branded.PaymentIntentData!.StatementDescriptorSuffix);
        Assert.Null(plain.BrandingSettings);
        Assert.Null(plain.PaymentIntentData!.StatementDescriptorSuffix);
    }

    [Fact]
    public void OffSessionCharge_KeepsTheSavedCard_WithTheSuffix()
    {
        PaymentIntentCreateOptions options = StripeCheckoutOptions.Charge(new PlatformCardChargeRequest("cus_1", "pm_1", 5400, "USD", "INV-2026-000002",
            new Dictionary<string, string>(), "vx4-2", "VIXLY"));

        Assert.Equal(new List<string> { "card" }, options.AllowedPaymentMethodTypes);
        Assert.Null(options.PaymentMethodTypes);
        Assert.Equal("pm_1", options.PaymentMethod);
        Assert.True(options.OffSession);
        Assert.Equal("VIXLY", options.StatementDescriptorSuffix);
    }

    [Theory]
    [InlineData("checkout.session.completed", "paid", PlatformCardEventKind.CheckoutCompleted)]
    [InlineData("checkout.session.completed", "unpaid", PlatformCardEventKind.CheckoutProcessing)]
    [InlineData("checkout.session.async_payment_succeeded", "paid", PlatformCardEventKind.CheckoutPaymentSucceeded)]
    [InlineData("checkout.session.async_payment_failed", "unpaid", PlatformCardEventKind.CheckoutPaymentFailed)]
    public void AsyncCheckoutEvents_Map(string type, string paymentStatus, PlatformCardEventKind expected)
    {
        Event stripeEvent = new()
        {
            Id = "evt_1",
            Type = type,
            Created = new DateTime(2026, 11, 2, 0, 0, 0, DateTimeKind.Utc),
            Data = new EventData
            {
                Object = new Session
                {
                    Id = "cs_1",
                    Mode = "payment",
                    PaymentStatus = paymentStatus,
                    PaymentIntentId = "pi_1",
                    Metadata = new Dictionary<string, string> { [PlatformCardMetadata.Transaction] = Guid.Empty.ToString("D") }
                }
            }
        };

        PlatformCardEvent mapped = StripeEvents.Map(stripeEvent);

        Assert.Equal(expected, mapped.Kind);
        Assert.Equal("cs_1", mapped.CheckoutId);
        Assert.Equal("pi_1", mapped.PaymentId);
    }
}
