using Vixly.Operations.Application.Branding;

namespace Vixly.Operations.Application.Ports;

// =========================================================
// FILE: IOperationsEdgeClient.cs
//
// PURPOSE:
// Sends a company's brand to the Cloudflare Worker, for its maintenance page (owner, 2026-10-23 W-M2 and 2026-10-28).
//
// RESPONSIBILITIES:
// - Send one company's name, colour, language and logo, signed
//
// IMPORTANT NOTES:
// - Not configured (no address or secret): NotConfigured, nothing sent, never an error
// - A refusal or an unreachable Worker throws, so the outbox tries again
// =========================================================
public interface IOperationsEdgeClient
{
    Task<OperationsEdgeSendResult> SendBrandAsync(OperationsEdgeBrand brand, CancellationToken cancellationToken);
}
