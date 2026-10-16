namespace Vixly.Platform.Infrastructure.Payments;

// =========================================================
// FILE: PlatformPaymentsOptions.cs
//
// PURPOSE:
// Configuration of card payments on this host (D-P1, D-P8).
//
// RESPONSIBILITIES:
// - Choose the provider and where the simulated provider sends its events
//
// IMPORTANT NOTES:
// - Secrets live in PlatformStripeOptions and come from environment variables only (S-11)
// - Simulated is refused outside Development
// =========================================================
public sealed class PlatformPaymentsOptions
{
    public const string SectionName = "PlatformPayments";

    // "Stripe" or "Simulated". Unset: Simulated in Development, Stripe everywhere else.
    public string? Provider { get; set; }

    public PlatformStripeOptions Stripe { get; set; } = new();

    // Development only: where the simulated checkout page lives (Platform.Api) and where its events go.
    public string SimulatedBaseUrl { get; set; } = "https://localhost:7402";

    // Development only; not a credential. Both hosts must use the same value.
    public string SimulatedWebhookSecret { get; set; } = "vixly-development-simulated-webhook";
}
