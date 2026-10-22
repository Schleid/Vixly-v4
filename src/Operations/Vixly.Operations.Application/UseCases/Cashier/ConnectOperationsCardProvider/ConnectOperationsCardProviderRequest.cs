namespace Vixly.Operations.Application.UseCases.Cashier.ConnectOperationsCardProvider;

// =========================================================
// FILE: ConnectOperationsCardProviderRequest.cs
//
// PURPOSE:
// Connects the company's own Stripe account with its keys (Slice 5F step 4; owner 2026-10-21).
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class ConnectOperationsCardProviderRequest
{
    public string SecretKey { get; init; } = string.Empty;

    public string PublishableKey { get; init; } = string.Empty;

    public string WebhookSecret { get; init; } = string.Empty;
}
