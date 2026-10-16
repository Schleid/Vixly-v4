namespace Vixly.Platform.Application.UseCases.Payments.ReceivePlatformProviderEvent;

// =========================================================
// FILE: ReceivePlatformProviderEventRequest.cs
//
// PURPOSE:
// Accepts a webhook from the card provider: checks its signature, keeps it once and queues it (D-P5).
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class ReceivePlatformProviderEventRequest
{
    public string Payload { get; init; } = string.Empty;

    public string? Signature { get; init; }
}
