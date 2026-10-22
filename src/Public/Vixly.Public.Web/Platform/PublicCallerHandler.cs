using System.Globalization;
using System.Net.Http.Headers;
using Microsoft.AspNetCore.Http;

namespace Vixly.Public.Web.Platform;

// =========================================================
// FILE: PublicCallerHandler.cs
//
// PURPOSE:
// Adds the visitor's signed IP and the page language to every Platform.Api call (Stage 2).
//
// RESPONSIBILITIES:
// - Set X-Vixly-Caller with a fresh token
// - Set Accept-Language to the page language
//
// IMPORTANT NOTES:
// - Authorization stays free for the registration access token
// =========================================================
public sealed class PublicCallerHandler : DelegatingHandler
{
    public const string Header = "X-Vixly-Caller";

    private readonly PublicCallerSigner _signer;

    private readonly IHttpContextAccessor _http;

    public PublicCallerHandler(PublicCallerSigner signer, IHttpContextAccessor http)
    {
        _signer = signer ?? throw new ArgumentNullException(nameof(signer));
        _http = http ?? throw new ArgumentNullException(nameof(http));
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (_signer.Sign(_http.HttpContext?.Connection.RemoteIpAddress?.ToString()) is { } token)
        {
            request.Headers.Remove(Header);
            request.Headers.TryAddWithoutValidation(Header, token);
        }

        request.Headers.AcceptLanguage.Clear();
        request.Headers.AcceptLanguage.Add(new StringWithQualityHeaderValue(CultureInfo.CurrentUICulture.TwoLetterISOLanguageName));

        return base.SendAsync(request, cancellationToken);
    }
}
