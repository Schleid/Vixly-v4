namespace Vixly.Platform.Web.Security;

// =========================================================
// FILE: PlatformCookies.cs
//
// PURPOSE:
// Names and settings of every cookie the console writes
// (Global Standard S-4).
//
// RESPONSIBILITIES:
// - Host-only __Host- names with V4 prefixes
// - One place to write and delete them with the same options
//
// IMPORTANT NOTES:
// - __Host- requires Secure, Path=/ and no Domain, so a V3
//   domain-wide cookie can never collide
// - Session: no Expires; the server enforces the 5-minute idle
//   and 12-hour absolute limits
// - Sign-in challenge: 5 minutes
// - The culture cookie is not secret but follows the same
//   host-only rule
// =========================================================
public static class PlatformCookies
{
    public const string Session = "__Host-vx4-auth";

    public const string SignInChallenge = "__Host-vx4-signin";

    public const string PasskeyCeremony = "__Host-vx4-pk";

    public const string Antiforgery = "__Host-vx4-af";

    public const string Culture = "__Host-vx4-lang";

    public static void Write(HttpResponse response, string name, string value, TimeSpan? lifetime)
    {
        ArgumentNullException.ThrowIfNull(response);

        response.Cookies.Append(name, value, Options(lifetime));
    }

    public static void Delete(HttpResponse response, string name)
    {
        ArgumentNullException.ThrowIfNull(response);

        response.Cookies.Delete(name, Options(null));
    }

    private static CookieOptions Options(TimeSpan? lifetime)
    {
        return new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Lax,
            Path = "/",
            IsEssential = true,
            MaxAge = lifetime
        };
    }
}
