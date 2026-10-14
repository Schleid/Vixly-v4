using Vixly.Platform.Application.Ports;

namespace Vixly.Platform.Api.Hosting;

// =========================================================
// FILE: PlatformApiRequestMetadata.cs
//
// PURPOSE:
// IPlatformRequestMetadata for API requests.
//
// RESPONSIBILITIES:
// - Expose the client IP, user agent and correlation id to audit
//
// IMPORTANT NOTES:
// - The IP has already been resolved by the forwarded-headers
//   middleware
// =========================================================
internal sealed class PlatformApiRequestMetadata : IPlatformRequestMetadata
{
    private readonly IHttpContextAccessor _accessor;

    public PlatformApiRequestMetadata(IHttpContextAccessor accessor)
    {
        _accessor = accessor ?? throw new ArgumentNullException(nameof(accessor));
    }

    public string? ClientIp => _accessor.HttpContext?.Connection.RemoteIpAddress?.ToString();

    public string? UserAgent => _accessor.HttpContext?.Request.Headers.UserAgent.ToString();

    public string? CorrelationId => _accessor.HttpContext?.TraceIdentifier;
}
