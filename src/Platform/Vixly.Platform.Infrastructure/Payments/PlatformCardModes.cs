using Vixly.Platform.Application.Payments;
using Vixly.Platform.Domain.Payments;

namespace Vixly.Platform.Infrastructure.Payments;

// =========================================================
// FILE: PlatformCardModes.cs
//
// PURPOSE:
// Decides whether the configured keys may be used on this host (D-P9).
//
// RESPONSIBILITIES:
// - Read test or live from a Stripe key
// - Refuse a live key outside Production and a test key in Production
//
// IMPORTANT NOTES:
// - Pure function; no secret is returned or logged
// =========================================================
public static class PlatformCardModes
{
    public static PlatformCardGatewayStatus Stripe(PlatformStripeOptions options, string environment)
    {
        ArgumentNullException.ThrowIfNull(options);

        string key = options.SecretKey?.Trim() ?? string.Empty;

        if (key.Length == 0 || string.IsNullOrWhiteSpace(options.WebhookSecret))
        {
            return new PlatformCardGatewayStatus(PlatformCardProvider.Stripe, false, null, PlatformPaymentsErrors.SecretMissing.Code);
        }

        PlatformPaymentMode? mode = key.StartsWith("sk_live_", StringComparison.Ordinal) || key.StartsWith("rk_live_", StringComparison.Ordinal) ? PlatformPaymentMode.Live
            : key.StartsWith("sk_test_", StringComparison.Ordinal) || key.StartsWith("rk_test_", StringComparison.Ordinal) ? PlatformPaymentMode.Test
            : null;

        if (mode is null)
        {
            return new PlatformCardGatewayStatus(PlatformCardProvider.Stripe, false, null, PlatformPaymentsErrors.SecretMissing.Code);
        }

        bool production = string.Equals(environment, "Production", StringComparison.OrdinalIgnoreCase);

        return production == (mode == PlatformPaymentMode.Live)
            ? new PlatformCardGatewayStatus(PlatformCardProvider.Stripe, true, mode, null)
            : new PlatformCardGatewayStatus(PlatformCardProvider.Stripe, false, mode, PlatformPaymentsErrors.ModeMismatch.Code);
    }
}
