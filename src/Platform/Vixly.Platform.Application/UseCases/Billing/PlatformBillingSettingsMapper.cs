using Vixly.Platform.Domain.Billing;

namespace Vixly.Platform.Application.UseCases.Billing;

// =========================================================
// FILE: PlatformBillingSettingsMapper.cs
//
// PURPOSE:
// Turns billing settings and the company profile into what the console shows.
//
// RESPONSIBILITIES:
// - Map domain records to items
//
// IMPORTANT NOTES:
// - No behaviour
// =========================================================
public static class PlatformBillingSettingsMapper
{
    public static PlatformBillingSettingsItem Item(PlatformBillingSettings settings, PlatformCompanyProfile? profile)
    {
        ArgumentNullException.ThrowIfNull(settings);

        return new PlatformBillingSettingsItem(
            settings.PaymentTermsDays,
            settings.InvoicePrefix,
            settings.CreditNotePrefix,
            settings.ReceiptPrefix,
            settings.PaperSize,
            settings.EmailDocuments,
            settings.Texts.ToDictionary(text => text.Language.Value, text => new PlatformBillingTextItem(text.PaymentInstructions, text.Footer), StringComparer.Ordinal),
            profile is null
                ? null
                : new PlatformCompanyProfileItem(
                    profile.Company.LegalName,
                    profile.Company.TradingName,
                    profile.Company.Email.Value,
                    profile.Company.Phone.Value,
                    profile.Company.Address.Line1,
                    profile.Company.Address.Line2,
                    profile.Company.Address.City,
                    profile.Company.Address.StateProvince,
                    profile.Company.Address.PostalCode,
                    profile.Company.Address.CountryCode,
                    profile.TaxId,
                    profile.Website,
                    profile.Logo is not null,
                    profile.SupportEmail,
                    profile.SupportPhone));
    }
}
