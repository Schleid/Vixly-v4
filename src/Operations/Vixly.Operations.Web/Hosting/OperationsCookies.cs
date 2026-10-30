namespace Vixly.Operations.Web.Hosting;

// =========================================================
// FILE: OperationsCookies.cs
//
// PURPOSE:
// Names of the cookies Operations.Web writes (S-4, Slice 0 D-F5).
//
// RESPONSIBILITIES:
// - Name every cookie in one place
//
// IMPORTANT NOTES:
// - Host-only (__Host-), Secure, HttpOnly, SameSite=Lax
// - Different from Platform and V3 names, so two apps on one host name never mix cookies
// =========================================================
public static class OperationsCookies
{
    public const string Session = "__Host-vx4-ops";

    public const string Device = "__Host-vx4-device";

    public const string Antiforgery = "__Host-vx4-ops-af";

    public const string Culture = "__Host-vx4-ops-lang";

    // Slice 5F step 3: a customer screen's own secret (kept 400 days, the most a browser keeps a cookie).
    public const string Screen = "__Host-vx4-screen";

    // Between the password and the second factor (5 minutes).
    public const string SignIn = "__Host-vx4-ops-signin";

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
