using Vixly.Customer.Web.Hosting;

namespace Vixly.Customer.Web.Portal;

// =========================================================
// FILE: CustomerSessionCookie.cs
//
// PURPOSE:
// The customer's session cookie: the token only (Slice 2, D-C4).
//
// RESPONSIBILITIES:
// - Read, write and delete the cookie
//
// IMPORTANT NOTES:
// - __Secure-, HttpOnly, SameSite=Lax, path /customer; ends with the browser after 12 hours at most
// =========================================================
public static class CustomerSessionCookie
{
    public static string? Read(HttpRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        // Base64url characters only: anything else is not a session of ours (review L8).
        return request.Cookies.TryGetValue(CustomerCookies.Session, out string? value) && value is { Length: > 0 and <= 200 }
            && value.All(character => char.IsAsciiLetterOrDigit(character) || character is '-' or '_')
            ? value
            : null;
    }

    public static void Write(HttpResponse response, string token)
    {
        ArgumentNullException.ThrowIfNull(response);
        response.Cookies.Append(CustomerCookies.Session, token, new CookieOptions
        {
            Path = CustomerCookies.Path,
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Lax,
            IsEssential = true,
            MaxAge = TimeSpan.FromHours(12)
        });
    }

    public static void Delete(HttpResponse response)
    {
        ArgumentNullException.ThrowIfNull(response);
        response.Cookies.Delete(CustomerCookies.Session, new CookieOptions { Path = CustomerCookies.Path, Secure = true, HttpOnly = true, SameSite = SameSiteMode.Lax });
    }
}
