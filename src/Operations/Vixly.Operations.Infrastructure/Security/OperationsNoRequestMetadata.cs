using Vixly.Operations.Application.Ports;

namespace Vixly.Operations.Infrastructure.Security;

// =========================================================
// FILE: OperationsNoRequestMetadata.cs
//
// PURPOSE:
// Request facts outside a web request (outbox, clocks).
//
// RESPONSIBILITIES:
// - Answer null for everything
//
// IMPORTANT NOTES:
// - The web host replaces it with the request-based one
// =========================================================
public sealed class OperationsNoRequestMetadata : IOperationsRequestMetadata
{
    public string? ClientIp => null;

    public string? UserAgent => null;

    public string? CorrelationId => null;
}
