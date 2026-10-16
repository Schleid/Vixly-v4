using Microsoft.AspNetCore.Http;

namespace Vixly.Platform.Api.Endpoints.Internal;

// =========================================================
// FILE: PlatformInternalCache.cs
//
// PURPOSE:
// How long a caller may keep an internal answer (Slice 9, D-K6).
//
// RESPONSIBILITIES:
// - Mark an answer private with a max age
//
// IMPORTANT NOTES:
// - Errors and writes stay no-store (set by the authentication step)
// =========================================================
public static class PlatformInternalCache
{
    public const int PortalSeconds = 60;

    public const int TenantSeconds = 300;

    public static IResult Ok<T>(HttpContext http, T value, int seconds)
    {
        ArgumentNullException.ThrowIfNull(http);

        http.Response.Headers.CacheControl = $"private, max-age={seconds}";
        return Results.Ok(value);
    }
}
