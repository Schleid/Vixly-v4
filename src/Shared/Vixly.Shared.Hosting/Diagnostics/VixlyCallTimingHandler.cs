using System.Diagnostics;
using Microsoft.AspNetCore.Http;

namespace Vixly.Shared.Hosting.Diagnostics;

// =========================================================
// FILE: VixlyCallTimingHandler.cs
//
// PURPOSE:
// Adds each outgoing call to the request that made it (performance plan P-1).
//
// RESPONSIBILITIES:
// - Time every call made through the HTTP client factory, such as Operations to Control
//
// IMPORTANT NOTES:
// - Calls outside a request (background jobs) are not counted; nothing about the call is logged here
// =========================================================
public sealed class VixlyCallTimingHandler : DelegatingHandler
{
    private readonly IHttpContextAccessor _http;

    public VixlyCallTimingHandler(IHttpContextAccessor http)
    {
        _http = http ?? throw new ArgumentNullException(nameof(http));
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (_http.HttpContext?.Features.Get<VixlyRequestTimes>() is not { } times)
        {
            return await base.SendAsync(request, cancellationToken);
        }

        long started = Stopwatch.GetTimestamp();

        try
        {
            return await base.SendAsync(request, cancellationToken);
        }
        finally
        {
            times.AddCall(Stopwatch.GetElapsedTime(started));
        }
    }
}
