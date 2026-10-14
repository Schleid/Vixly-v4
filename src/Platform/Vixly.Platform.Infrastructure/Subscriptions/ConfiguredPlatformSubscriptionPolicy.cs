using Microsoft.Extensions.Options;
using Vixly.Platform.Application.Ports;

namespace Vixly.Platform.Infrastructure.Subscriptions;

// =========================================================
// FILE: ConfiguredPlatformSubscriptionPolicy.cs
//
// PURPOSE:
// Reads subscription rules from configuration.
//
// RESPONSIBILITIES:
// - Expose the configured unpaid read-only period
//
// IMPORTANT NOTES:
// - Negative values are treated as zero
// =========================================================
internal sealed class ConfiguredPlatformSubscriptionPolicy : IPlatformSubscriptionPolicy
{
    private readonly PlatformSubscriptionOptions _options;

    public ConfiguredPlatformSubscriptionPolicy(IOptions<PlatformSubscriptionOptions> options)
    {
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
    }

    public TimeSpan UnpaidReadOnlyPeriod => TimeSpan.FromDays(Math.Max(0, _options.UnpaidReadOnlyDays));
}
