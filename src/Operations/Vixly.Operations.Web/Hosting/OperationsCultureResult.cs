using System.Globalization;

namespace Vixly.Operations.Web.Hosting;

// =========================================================
// FILE: OperationsCultureResult.cs
//
// PURPOSE:
// Renders a page in a given language (Slice 5F step 3): the customer screen speaks the customer's language, not the browser's.
//
// RESPONSIBILITIES:
// - Run the inner result with that culture, then put the request's back
//
// IMPORTANT NOTES:
// - Only English, French and Spanish; anything else keeps the request's language
// =========================================================
public sealed class OperationsCultureResult : IResult
{
    private static readonly string[] Supported = { "en", "fr", "es" };

    private readonly IResult _inner;

    private readonly string? _language;

    public OperationsCultureResult(IResult inner, string? language)
    {
        _inner = inner ?? throw new ArgumentNullException(nameof(inner));
        _language = language;
    }

    public async Task ExecuteAsync(HttpContext httpContext)
    {
        // "es-MX", "FR": the language part (review).
        string? language = _language is { Length: >= 2 } ? _language[..2].ToLowerInvariant() : null;

        if (language is null || !Supported.Contains(language, StringComparer.Ordinal))
        {
            await _inner.ExecuteAsync(httpContext);
            return;
        }

        (CultureInfo culture, CultureInfo uiCulture) = (CultureInfo.CurrentCulture, CultureInfo.CurrentUICulture);
        CultureInfo.CurrentCulture = CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo(language);

        try
        {
            await _inner.ExecuteAsync(httpContext);
        }
        finally
        {
            (CultureInfo.CurrentCulture, CultureInfo.CurrentUICulture) = (culture, uiCulture);
        }
    }
}
