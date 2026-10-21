using Microsoft.AspNetCore.Localization;
using Vixly.Shared.Localization;

namespace Vixly.Operations.Web.Tenancy;

// =========================================================
// FILE: OperationsTenantCultureProvider.cs
//
// PURPOSE:
// Uses the tenant default language when the person has not chosen one (L-4).
//
// RESPONSIBILITIES:
// - Answer the tenant default language when it is supported
//
// IMPORTANT NOTES:
// - Order (L-4): the person choice (cookie), then this, then the browser
// =========================================================
public sealed class OperationsTenantCultureProvider : RequestCultureProvider
{
    public override Task<ProviderCultureResult?> DetermineProviderCultureResult(HttpContext httpContext)
    {
        ArgumentNullException.ThrowIfNull(httpContext);

        string? language = OperationsTenantMiddleware.Host(httpContext)?.DefaultLanguage;

        return Task.FromResult(language is not null && VixlyCultures.Supported.Any(culture => culture.TwoLetterISOLanguageName == language)
            ? new ProviderCultureResult(language)
            : null);
    }
}
