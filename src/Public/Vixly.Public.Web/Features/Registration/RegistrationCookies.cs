using Microsoft.AspNetCore.Http;

namespace Vixly.Public.Web.Features.Registration;

// =========================================================
// FILE: RegistrationCookies.cs
//
// PURPOSE:
// Keeps the registration access token and an offer token for the registrant (Slice 2, S-4).
//
// RESPONSIBILITIES:
// - Read, write and clear the host-only cookies
//
// IMPORTANT NOTES:
// - HttpOnly, Secure, SameSite=Lax, path /; the access token lasts 7 days
// - Only ever sent server to server as a bearer token
// =========================================================
public static class RegistrationCookies
{
    public const string Access = "__Host-vx4-registration";

    public const string Offer = "__Host-vx4-offer";

    public static string? Read(HttpContext? http, string name)
    {
        string? value = http?.Request.Cookies[name];
        return string.IsNullOrWhiteSpace(value) ? null : value;
    }

    public static void Write(HttpContext http, string name, string value, DateTimeOffset? expiresUtc)
    {
        ArgumentNullException.ThrowIfNull(http);

        http.Response.Cookies.Append(name, value, new CookieOptions
        {
            Path = "/",
            Secure = true,
            HttpOnly = true,
            SameSite = SameSiteMode.Lax,
            IsEssential = true,
            Expires = expiresUtc is { } expires && expires > DateTimeOffset.UtcNow ? expires : DateTimeOffset.UtcNow.AddDays(7)
        });
    }

    public static void Clear(HttpContext http, string name)
    {
        ArgumentNullException.ThrowIfNull(http);
        http.Response.Cookies.Delete(name, new CookieOptions { Path = "/", Secure = true, HttpOnly = true, SameSite = SameSiteMode.Lax });
    }
}
