namespace Vixly.Operations.Application.UseCases.Cashier.HandleOperationsCardWebhook;

// =========================================================
// FILE: HandleOperationsCardWebhookRequest.cs
//
// PURPOSE:
// Takes an event from the company's Stripe account and records a card payment it confirms (Slice 5F step 4).
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class HandleOperationsCardWebhookRequest
{
    public string Payload { get; init; } = string.Empty;

    public string? Signature { get; init; }
}
