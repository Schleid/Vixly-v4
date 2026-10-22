using System.Globalization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Localization;
using Vixly.Shared.Localization;

namespace Vixly.Public.Web.Hosting;

// =========================================================
// FILE: PublicLanguage.cs
//
// PURPOSE:
// Language of a public page: the URL first, then the cookie, then the browser (ADR-0005).
//
// RESPONSIBILITIES:
// - Read the language from the first path segment
// - Build page links and alternates in a language
// - Redirect the root to a language and remember the choice
//
// IMPORTANT NOTES:
// - Links from emails carry no language and use the cookie or the browser
// - A language page asked without a language (/register?offer=…) is redirected to the visitor's language
// - The cookie is host-only and not sensitive (S-4)
// =========================================================
public static class PublicLanguage
{
    public const string CookieName = "__Host-vx4-culture";

    private static readonly string[] LanguagePages = { "/register", "/plans", "/contact", "/features", "/legal" };

    public static IReadOnlyList<string> Codes { get; } = VixlyCultures.Supported.Select(culture => culture.TwoLetterISOLanguageName).ToArray();

    public static string Current => Codes.Contains(CultureInfo.CurrentUICulture.TwoLetterISOLanguageName) ? CultureInfo.CurrentUICulture.TwoLetterISOLanguageName : "en";

    public static string? FromPath(PathString path)
    {
        string? first = path.Value?.Split('/', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();
        return first is not null && Codes.Contains(first, StringComparer.Ordinal) ? first : null;
    }

    // "/fr/plans" -> "/es/plans"; a path without a language gets one.
    public static string Swap(string pathAndQuery, string language)
    {
        string path = string.IsNullOrEmpty(pathAndQuery) ? "/" : pathAndQuery;
        string? current = FromPath(new PathString(path.Split('?')[0]));
        return current is null ? $"/{language}/" : $"/{language}" + path[(current.Length + 1)..];
    }

    public static string Page(string language, string page = "")
    {
        return string.IsNullOrEmpty(page) ? $"/{language}/" : $"/{language}/{page}";
    }

    public static RequestCultureProvider PathProvider { get; } = new CustomRequestCultureProvider(http =>
    {
        string? language = FromPath(http.Request.Path);
        return Task.FromResult(language is null ? null : new ProviderCultureResult(language));
    });

    public static IApplicationBuilder UsePublicLanguage(this IApplicationBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        return app.Use(async (http, next) =>
        {
            if (http.Request.Path == "/" && HttpMethods.IsGet(http.Request.Method))
            {
                http.Response.Headers.Vary = "Accept-Language, Cookie";
                http.Response.Redirect(Page(Current));
                return;
            }

            // A page from an email link has no language in its address: switch through the cookie.
            if (FromPath(http.Request.Path) is null && http.Request.Query["lang"].ToString() is { Length: 2 } chosen && Codes.Contains(chosen, StringComparer.Ordinal))
            {
                Remember(http, chosen);
                Dictionary<string, Microsoft.Extensions.Primitives.StringValues> rest = http.Request.Query.Where(pair => pair.Key != "lang").ToDictionary(pair => pair.Key, pair => pair.Value);
                http.Response.Redirect(Microsoft.AspNetCore.WebUtilities.QueryHelpers.AddQueryString(http.Request.Path, rest));
                return;
            }

            // Links Platform sends without a language (an offer link: /register?offer=…) go to the visitor's language.
            if (FromPath(http.Request.Path) is null && HttpMethods.IsGet(http.Request.Method) && LanguagePages.Contains(http.Request.Path.Value ?? string.Empty, StringComparer.OrdinalIgnoreCase))
            {
                http.Response.Redirect(Page(Current, http.Request.Path.Value!.TrimStart('/').ToLowerInvariant()) + http.Request.QueryString.Value);
                return;
            }

            if (FromPath(http.Request.Path) is { } language)
            {
                Remember(http, language);
            }

            await next(http);
        });
    }

    private static void Remember(HttpContext http, string language)
    {
        string wanted = CookieRequestCultureProvider.MakeCookieValue(new RequestCulture(language));

        if (http.Request.Cookies[CookieName] != wanted)
        {
            http.Response.Cookies.Append(CookieName, wanted, new CookieOptions
            {
                Path = "/",
                Secure = true,
                HttpOnly = true,
                SameSite = SameSiteMode.Lax,
                IsEssential = true,
                MaxAge = TimeSpan.FromDays(365)
            });
        }
    }
}
