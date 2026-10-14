using Vixly.Platform.Application.Ports;

namespace Vixly.Platform.Web.Security;

// =========================================================
// FILE: PlatformHttpRequestMetadata.cs
//
// PURPOSE:
// Tells the audit trail the client IP, user agent and
// correlation id of the current request.
//
// RESPONSIBILITIES:
// - Inside an interactive circuit: use the facts captured when
//   the circuit opened
// - Otherwise: read them from HttpContext
//
// IMPORTANT NOTES:
// - The client IP is the one ForwardedHeaders resolved from
//   Cloudflare (ADR-0011), never a header the browser sent
// =========================================================
public sealed class PlatformHttpRequestMetadata : IPlatformRequestMetadata
{
    private readonly IHttpContextAccessor _accessor;

    private readonly PlatformInteractiveRequest _interactive;

    public PlatformHttpRequestMetadata(IHttpContextAccessor accessor, PlatformInteractiveRequest interactive)
    {
        _accessor = accessor ?? throw new ArgumentNullException(nameof(accessor));
        _interactive = interactive ?? throw new ArgumentNullException(nameof(interactive));
    }

    public string? ClientIp => _interactive.IsCaptured
        ? _interactive.ClientIp
        : _accessor.HttpContext?.Connection.RemoteIpAddress?.ToString();

    public string? UserAgent => _interactive.IsCaptured
        ? _interactive.UserAgent
        : _accessor.HttpContext?.Request.Headers.UserAgent.ToString();

    public string? CorrelationId => _interactive.IsCaptured
        ? _interactive.CorrelationId
        : _accessor.HttpContext?.TraceIdentifier;
}
