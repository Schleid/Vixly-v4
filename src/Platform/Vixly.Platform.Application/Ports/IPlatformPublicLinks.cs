namespace Vixly.Platform.Application.Ports;

// =========================================================
// FILE: IPlatformPublicLinks.cs
//
// PURPOSE:
// Builds links to the public site.
//
// RESPONSIBILITIES:
// - Build the registration link of an offer (D-S21)
//
// IMPORTANT NOTES:
// - Implemented in Infrastructure from the hosting settings; the token is URL-escaped
// =========================================================
public interface IPlatformPublicLinks
{
    string OfferLink(string token);

    // Slice 5: the public pages that open the card provider's hosted checkout.
    string PayLink(string token);

    string SaveCardLink(string token);

    string RegistrationPayment();

    // Slice 7: pages on the tenant host; the token goes after "#" so it never reaches server logs.
    string DeviceLink(string portalSlug, Vixly.Platform.Domain.Licensing.PlatformDeviceLinkKind kind, string token);

    // The tenant portal page where the owner reads and accepts the Terms (Slice 8).
    string LegalLink(string portalSlug);
}
