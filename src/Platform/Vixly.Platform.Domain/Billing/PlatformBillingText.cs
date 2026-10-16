using Vixly.Shared.Kernel.Localization;

namespace Vixly.Platform.Domain.Billing;

// =========================================================
// FILE: PlatformBillingText.cs
//
// PURPOSE:
// Payment instructions and footer printed on invoices in one language (D-B4).
//
// RESPONSIBILITIES:
// - Hold the two texts for one language
//
// IMPORTANT NOTES:
// - Owned by PlatformBillingSettings
// =========================================================
public sealed class PlatformBillingText
{
    private PlatformBillingText()
    {
        Language = LanguageCode.Default;
    }

    internal PlatformBillingText(LanguageCode language, string? paymentInstructions, string? footer)
    {
        Language = language;
        PaymentInstructions = paymentInstructions;
        Footer = footer;
    }

    public LanguageCode Language { get; private set; }

    public string? PaymentInstructions { get; private set; }

    public string? Footer { get; private set; }
}
