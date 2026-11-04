using Microsoft.AspNetCore.OutputCaching;
using Microsoft.Extensions.Primitives;

namespace Vixly.Public.Web.Hosting;

// =========================================================
// FILE: PublicHelpCachePolicy.cs
//
// PURPOSE:
// Caches public help while keeping searches out of the cache.
//
// RESPONSIBILITIES:
// - Bound article/home caching to successful read responses.
//
// IMPORTANT NOTES:
// - Never store a response carrying Set-Cookie or a search query.
// - The language is part of every help route and cache key.
// =========================================================
public sealed class PublicHelpCachePolicy : IOutputCachePolicy
{
    public static PublicHelpCachePolicy Instance { get; } = new();

    public ValueTask CacheRequestAsync(OutputCacheContext context, CancellationToken cancellationToken)
    {
        bool cache = HttpMethods.IsGet(context.HttpContext.Request.Method)
            && !context.HttpContext.Request.Query.ContainsKey("q");
        context.EnableOutputCaching = cache;
        context.AllowCacheLookup = cache;
        context.AllowCacheStorage = cache;
        context.AllowLocking = cache;
        context.ResponseExpirationTimeSpan = TimeSpan.FromMinutes(10);
        context.CacheVaryByRules.QueryKeys = new StringValues("*");
        return ValueTask.CompletedTask;
    }

    public ValueTask ServeFromCacheAsync(OutputCacheContext context, CancellationToken cancellationToken) => ValueTask.CompletedTask;

    public ValueTask ServeResponseAsync(OutputCacheContext context, CancellationToken cancellationToken)
    {
        if (context.HttpContext.Response.StatusCode != StatusCodes.Status200OK
            || context.HttpContext.Response.Headers.ContainsKey("Set-Cookie")) context.AllowCacheStorage = false;
        return ValueTask.CompletedTask;
    }
}
