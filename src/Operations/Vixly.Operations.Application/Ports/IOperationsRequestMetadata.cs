namespace Vixly.Operations.Application.Ports;

// =========================================================
// FILE: IOperationsRequestMetadata.cs
//
// PURPOSE:
// Facts about the current request for audit and sessions.
//
// RESPONSIBILITIES:
// - Give the client IP, user agent and correlation id
//
// IMPORTANT NOTES:
// - Null outside a request (clocks, outbox)
// =========================================================
public interface IOperationsRequestMetadata
{
    string? ClientIp { get; }

    string? UserAgent { get; }

    string? CorrelationId { get; }
}
