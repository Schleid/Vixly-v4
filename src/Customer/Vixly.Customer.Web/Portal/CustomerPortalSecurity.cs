namespace Vixly.Customer.Web.Portal;

// =========================================================
// FILE: CustomerPortalSecurity.cs
//
// PURPOSE:
// The customer portal's content security policy (Slice 2).
//
// RESPONSIBILITIES:
// - Allow the bot check script and frame; everything else stays on the site
//
// IMPORTANT NOTES:
// - Like the public site (Stage 2): Turnstile is the only outside origin
// =========================================================
public static class CustomerPortalSecurity
{
    private const string Turnstile = "https://challenges.cloudflare.com";

    public const string ContentSecurityPolicy =
        "default-src 'self'; " +
        "script-src 'self' " + Turnstile + "; " +
        "style-src 'self' 'unsafe-inline'; " +
        "img-src 'self' data:; " +
        "font-src 'self'; " +
        "connect-src 'self'; " +
        "frame-src " + Turnstile + "; " +
        "frame-ancestors 'none'; " +
        "base-uri 'self'; " +
        "form-action 'self'; " +
        "object-src 'none'";
}
