namespace Vixly.Platform.Application.Ports;

// =========================================================
// FILE: IPlatformRequestMetadata.cs
//
// PURPOSE:
// Facts about the current request that the audit log and
// sessions record.
//
// RESPONSIBILITIES:
// - Client IP (the real one behind Cloudflare, ADR-0011)
// - User agent and correlation id
//
// IMPORTANT NOTES:
// - Implemented by each host; values are informational and
//   never used for authorization
// =========================================================
public interface IPlatformRequestMetadata
{
    string? ClientIp { get; }

    string? UserAgent { get; }

    string? CorrelationId { get; }
}
