namespace Vixly.Platform.Infrastructure.Payments;

// =========================================================
// FILE: PlatformStripeOptions.cs
//
// PURPOSE:
// The Stripe keys, read from configuration (D-P8, S-11).
//
// RESPONSIBILITIES:
// - Hold the secret key and the webhook signing secret
//
// IMPORTANT NOTES:
// - Set by environment variables PlatformPayments__Stripe__SecretKey and PlatformPayments__Stripe__WebhookSecret; never in a file, never logged
// =========================================================
public sealed class PlatformStripeOptions
{
    public string SecretKey { get; set; } = string.Empty;

    public string WebhookSecret { get; set; } = string.Empty;

    public override string ToString() => "PlatformStripeOptions (secrets hidden)";
}
