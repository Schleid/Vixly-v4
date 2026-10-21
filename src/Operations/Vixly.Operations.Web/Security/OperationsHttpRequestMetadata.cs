using Vixly.Operations.Application.Ports;

namespace Vixly.Operations.Web.Security;

// =========================================================
// FILE: OperationsHttpRequestMetadata.cs
//
// PURPOSE:
// Request facts for audit and sessions, from the current HTTP request.
//
// RESPONSIBILITIES:
// - Give the client IP (behind Cloudflare, ADR-0011), user agent and trace id
//
// IMPORTANT NOTES:
// - Null outside a request
// =========================================================
public sealed class OperationsHttpRequestMetadata : IOperationsRequestMetadata
{
    private readonly IHttpContextAccessor _http;

    public OperationsHttpRequestMetadata(IHttpContextAccessor http)
    {
        _http = http ?? throw new ArgumentNullException(nameof(http));
    }

    public string? ClientIp => _http.HttpContext?.Connection.RemoteIpAddress?.ToString();

    public string? UserAgent => _http.HttpContext?.Request.Headers.UserAgent.ToString();

    public string? CorrelationId => _http.HttpContext?.TraceIdentifier;
}
