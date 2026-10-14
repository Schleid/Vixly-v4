using Microsoft.AspNetCore.Localization;

namespace Vixly.Platform.Web.Security;

// =========================================================
// FILE: PlatformAccountCultureProvider.cs
//
// PURPOSE:
// Shows the console in the signed-in user's preferred language
// (ADR-0005).
//
// RESPONSIBILITIES:
// - Read the language claim the session handler added
// - Answer nothing for anonymous requests, so the language cookie
//   and the browser decide on the sign-in pages
//
// IMPORTANT NOTES:
// - Needs authentication to run before request localization
// - The claim is read fresh from the database on every request,
//   so a change in My account applies on the next page load
// =========================================================
public sealed class PlatformAccountCultureProvider : RequestCultureProvider
{
    public override Task<ProviderCultureResult?> DetermineProviderCultureResult(HttpContext httpContext)
    {
        ArgumentNullException.ThrowIfNull(httpContext);

        string? language = httpContext.User.FindFirst(PlatformClaimTypes.Language)?.Value;

        return Task.FromResult(string.IsNullOrEmpty(language) ? null : new ProviderCultureResult(language));
    }
}
