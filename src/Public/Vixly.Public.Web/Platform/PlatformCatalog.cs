using Microsoft.Extensions.Caching.Memory;
using Vixly.Contracts.Platform.Common;
using Vixly.Contracts.Platform.Legal;
using Vixly.Contracts.Platform.Registration;
using Vixly.Public.Web.Hosting;

namespace Vixly.Public.Web.Platform;

// =========================================================
// FILE: PlatformCatalog.cs
//
// PURPOSE:
// Plans and current Terms, kept 5 minutes per language (Stage 2, P-5).
//
// RESPONSIBILITIES:
// - Read the plan catalogue and the Terms through a short cache
//
// IMPORTANT NOTES:
// - Failures are not cached, so the next visitor tries again
// =========================================================
public sealed class PlatformCatalog
{
    private static readonly TimeSpan Lifetime = TimeSpan.FromMinutes(5);

    private readonly PlatformApi _api;

    private readonly IMemoryCache _cache;

    public PlatformCatalog(PlatformApi api, IMemoryCache cache)
    {
        _api = api ?? throw new ArgumentNullException(nameof(api));
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
    }

    public Task<PlatformApiResult<IReadOnlyList<PlatformPublicPlan>>> PlansAsync(CancellationToken cancellationToken)
    {
        return CachedAsync("plans:" + PublicLanguage.Current, () => _api.PlansAsync(cancellationToken));
    }

    public Task<PlatformApiResult<PlatformLegalTermsInfo>> TermsAsync(CancellationToken cancellationToken)
    {
        string language = PublicLanguage.Current;
        return CachedAsync("terms:" + language, () => _api.TermsAsync(language, cancellationToken));
    }

    private async Task<PlatformApiResult<T>> CachedAsync<T>(string key, Func<Task<PlatformApiResult<T>>> load)
    {
        if (_cache.TryGetValue(key, out PlatformApiResult<T>? cached) && cached is not null)
        {
            return cached;
        }

        PlatformApiResult<T> result = await load();

        if (result.Succeeded)
        {
            _cache.Set(key, result, Lifetime);
        }

        return result;
    }
}
