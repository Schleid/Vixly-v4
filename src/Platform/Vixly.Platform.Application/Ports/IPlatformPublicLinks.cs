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
}
