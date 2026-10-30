using Microsoft.AspNetCore.Localization;
using Vixly.Operations.Web.Site;

namespace Vixly.Operations.Web.Tenancy;

// =========================================================
// FILE: OperationsSitePathCultureProvider.cs
//
// PURPOSE:
// Takes the language of a tenant site page from its address (/en, /fr/locations ...).
//
// RESPONSIBILITIES:
// - Read the first path segment when it is a site language
//
// IMPORTANT NOTES:
// - Comes before the cookie and the tenant default, so each address always shows its own language (SEO)
// =========================================================
public sealed class OperationsSitePathCultureProvider : RequestCultureProvider
{
    public override Task<ProviderCultureResult?> DetermineProviderCultureResult(HttpContext httpContext)
    {
        ArgumentNullException.ThrowIfNull(httpContext);

        string? language = OperationsSiteLanguages.FromPath(httpContext.Request.Path);
        return Task.FromResult(language is null ? null : new ProviderCultureResult(language));
    }
}
