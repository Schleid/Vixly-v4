using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Vixly.Contracts.Platform.Internal.Entitlements;
using Vixly.Operations.Application.Platform;
using Vixly.Operations.Application.Ports;

namespace Vixly.Operations.Infrastructure.Platform;

// =========================================================
// FILE: OperationsPlanFeaturesClient.cs
//
// PURPOSE:
// Reads the branding features and partner agents of a tenant's plan from Platform's entitlements (W-4, Slice 7 step 4).
//
// RESPONSIBILITIES:
// - Keep each answer 5 minutes per tenant
// - Answer the branding features when Platform cannot be reached, so the site never breaks; partner agents stay off (R-8)
//
// IMPORTANT NOTES:
// - A plan change shows on the site, and for partner agents, within 5 minutes
// =========================================================
public sealed class OperationsPlanFeaturesClient : IOperationsPlanFeatures
{
    private static readonly TimeSpan Lifetime = TimeSpan.FromMinutes(5);

    private readonly OperationsPlatformSender _sender;

    private readonly IMemoryCache _cache;

    private readonly ILogger<OperationsPlanFeaturesClient> _logger;

    public OperationsPlanFeaturesClient(OperationsPlatformSender sender, IMemoryCache cache, ILogger<OperationsPlanFeaturesClient> logger)
    {
        _sender = sender ?? throw new ArgumentNullException(nameof(sender));
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<OperationsPlanFeatures> GetAsync(Guid tenantId, CancellationToken cancellationToken)
    {
        string key = "ops:plan-features:" + tenantId.ToString("N");

        if (_cache.TryGetValue(key, out OperationsPlanFeatures? cached) && cached is not null)
        {
            return cached;
        }

        try
        {
            (PlatformEntitlementsInfo? info, _) = await _sender.SendAsync<PlatformEntitlementsInfo>(HttpMethod.Get, "internal/v1/tenant/entitlements", tenantId, null, null, cancellationToken);

            if (info is null)
            {
                return OperationsPlanFeatures.Unknown;
            }

            OperationsPlanFeatures features = new(
                info.Features.Contains(OperationsPlanFeatures.Theme, StringComparer.Ordinal),
                info.Features.Contains(OperationsPlanFeatures.Content, StringComparer.Ordinal))
            {
                PartnerAgents = info.Features.Contains(OperationsPlanFeatures.Partners, StringComparer.Ordinal)
            };
            _cache.Set(key, features, Lifetime);
            return features;
        }
        catch (OperationsPlatformUnavailableException exception)
        {
            _logger.LogWarning(exception, "Plan features could not be read; the site shows the tenant's own choices and partner agents are refused.");
            return OperationsPlanFeatures.Unknown;
        }
    }
}
