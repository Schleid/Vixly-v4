using Vixly.Contracts.Platform.Legal;
using Vixly.Contracts.Platform.Registration;

namespace Vixly.Public.Web.Features.Registration;

// =========================================================
// FILE: RegistrationStartBody.cs
//
// PURPOSE:
// Turns the sign-up form into Platform's start body with the current Terms and Privacy versions (Slice 8).
//
// RESPONSIBILITIES:
// - Copy the fields and add the versions the registrant accepted
//
// IMPORTANT NOTES:
// - The versions come from Platform's current Terms, never from the form
// =========================================================
public static class RegistrationStartBody
{
    public static StartPlatformRegistrationBody From(StartFormModel form, PlatformLegalTermsInfo? terms, string language, string? botToken)
    {
        ArgumentNullException.ThrowIfNull(form);

        return new StartPlatformRegistrationBody
        {
            LegalName = form.LegalName ?? string.Empty,
            TradingName = form.TradingName,
            CompanyEmail = form.CompanyEmail ?? string.Empty,
            CompanyPhone = form.CompanyPhone ?? string.Empty,
            AddressLine1 = form.AddressLine1 ?? string.Empty,
            AddressLine2 = form.AddressLine2,
            City = form.City ?? string.Empty,
            StateProvince = form.StateProvince,
            PostalCode = form.PostalCode,
            CountryCode = form.CountryCode,
            OwnerName = form.OwnerName ?? string.Empty,
            OwnerEmail = form.OwnerEmail ?? string.Empty,
            OwnerPhone = form.OwnerPhone ?? string.Empty,
            Language = language,
            Slug = (form.Slug ?? string.Empty).Trim().ToLowerInvariant(),
            AcceptedTerms = form.AcceptedTerms,
            TermsVersion = terms?.Version ?? string.Empty,
            PrivacyVersion = terms?.Parts.FirstOrDefault(part => part.Kind == "privacy")?.Version ?? string.Empty,
            BotToken = botToken,
            OfferToken = string.IsNullOrWhiteSpace(form.OfferToken) ? null : form.OfferToken.Trim()
        };
    }
}
