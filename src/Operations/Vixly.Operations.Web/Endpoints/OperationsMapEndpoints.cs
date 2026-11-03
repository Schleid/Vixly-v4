using Vixly.Operations.Infrastructure.Maps;
using Vixly.Operations.Web.Hosting;

namespace Vixly.Operations.Web.Endpoints;

// =========================================================
// FILE: OperationsMapEndpoints.cs
//
// PURPOSE:
// The map pictures of the live location maps (Slice 12C, owner H-5).
//
// RESPONSIBILITIES:
// - /maps/tiles/{z}/{x}/{y}.png: one tile from the configured provider, through the site
//
// IMPORTANT NOTES:
// - Anonymous (the customer portal's map and the private link use it); rate-limited per address (MapTiles)
// - Kept by browsers for a day; no map configured: 404
// =========================================================
public static class OperationsMapEndpoints
{
    public static IEndpointRouteBuilder MapOperationsMapEndpoints(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        endpoints.MapGet("/maps/tiles/{z:int}/{x:int}/{y:int}.png", async (HttpContext http, int z, int x, int y, OperationsMapTiles tiles) =>
        {
            if (await tiles.GetAsync(z, x, y, http.RequestAborted) is not { } tile)
            {
                return Results.NotFound();
            }

            http.Response.Headers.CacheControl = "public, max-age=86400";
            return Results.File(tile, "image/png");
        }).RequireRateLimiting(OperationsRateLimits.MapTiles);

        return endpoints;
    }
}
