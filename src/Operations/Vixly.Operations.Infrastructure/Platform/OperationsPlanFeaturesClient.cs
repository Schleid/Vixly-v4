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
// - Keep each answer 5 minutes per tenant, then refresh it in the background while it is still served, never beyond 10 minutes (P-4)
// - Answer the branding features when Platform cannot be reached, so the site never breaks; partner agents stay off (R-8)
// - Read the shipment cap (0 when Platform cannot be reached: a counter never stops because of Platform)
//
// IMPORTANT NOTES:
// - A plan change shows on the site, and for partner agents, within 5 minutes (a little more while the background call runs)
// =========================================================
public sealed class OperationsPlanFeaturesClient : IOperationsPlanFeatures
{
    private static readonly TimeSpan Lifetime = TimeSpan.FromMinutes(5);

    private readonly OperationsPlatformSender _sender;

    private readonly IMemoryCache _cache;

    private readonly ILogger<OperationsPlanFeaturesClient> _logger;

    private readonly OperationsStaleWhileRefresh _fresh;

    public OperationsPlanFeaturesClient(OperationsPlatformSender sender, IMemoryCache cache, ILogger<OperationsPlanFeaturesClient> logger, OperationsStaleWhileRefresh fresh)
    {
        _sender = sender ?? throw new ArgumentNullException(nameof(sender));
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _fresh = fresh ?? throw new ArgumentNullException(nameof(fresh));
    }

    public async Task<OperationsPlanFeatures> GetAsync(Guid tenantId, CancellationToken cancellationToken)
    {
        string key = "ops:plan-features:" + tenantId.ToString("N");

        long generation = _fresh.Generation(key);

        if (_fresh.TryGet<OperationsPlanFeatures>(key, Lifetime, token => RefreshAsync(key, tenantId, token), out OperationsPlanFeatures kept))
        {
            return kept;
        }

        try
        {
            OperationsPlanFeatures? features = await ReadAsync(tenantId, cancellationToken);

            if (features is null)
            {
                return OperationsPlanFeatures.Unknown;
            }

            _fresh.Keep(key, features, generation);
            return features;
        }
        catch (OperationsPlatformUnavailableException exception)
        {
            _logger.LogWarning(exception, "Plan features could not be read; the site shows the tenant's own choices and partner agents are refused.");
            return OperationsPlanFeatures.Unknown;
        }
    }

    // The background call: no answer for the tenant any more drops the kept one, so the next page asks again (review, 2026-11-04).
    private async Task<OperationsPlanFeatures?> RefreshAsync(string key, Guid tenantId, CancellationToken cancellationToken)
    {
        OperationsPlanFeatures? features = await ReadAsync(tenantId, cancellationToken);

        if (features is null)
        {
            _fresh.Forget(key);
        }

        return features;
    }

    // One call to Platform; null when it has no answer for the tenant (nothing is kept).
    private async Task<OperationsPlanFeatures?> ReadAsync(Guid tenantId, CancellationToken cancellationToken)
    {
        (PlatformEntitlementsInfo? info, _) = await _sender.SendAsync<PlatformEntitlementsInfo>(HttpMethod.Get, "internal/v1/tenant/entitlements", tenantId, null, null, cancellationToken);

        if (info is null)
        {
            return null;
        }

        OperationsPlanFeatures features = new(
            info.Features.Contains(OperationsPlanFeatures.Theme, StringComparer.Ordinal),
            info.Features.Contains(OperationsPlanFeatures.Content, StringComparer.Ordinal))
        {
            PartnerAgents = info.Features.Contains(OperationsPlanFeatures.Partners, StringComparer.Ordinal),
            TextMessages = info.Features.Contains(OperationsPlanFeatures.Texts, StringComparer.Ordinal),
            QuickBooksSync = info.Features.Contains(OperationsPlanFeatures.QuickBooks, StringComparer.Ordinal),
            TenantApi = info.Features.Contains(OperationsPlanFeatures.Api, StringComparer.Ordinal),
            CarrierData = info.Features.Contains(OperationsPlanFeatures.Carriers, StringComparer.Ordinal),
            ShipmentCap = info.Limits is not null && info.Limits.TryGetValue(OperationsPlanFeatures.ShipmentsLimit, out int cap) && cap > 0 ? cap : 0,
            ExtraShipmentPrice = info.ExtraShipmentPrice,
            CurrencyCode = info.CurrencyCode,

            // A company without a subscription keeps every module; a plan without a module's feature leaves it out.
            ModulesOff = info.Status is null
                ? new HashSet<string>(StringComparer.Ordinal)
                : Vixly.Operations.Application.Plans.OperationsPlanModules.All.Where(module => !info.Features.Contains(module, StringComparer.Ordinal)).ToHashSet(StringComparer.Ordinal)
        };
        return features;
    }
}
