namespace Vixly.Operations.Infrastructure.Maps;

// =========================================================
// FILE: OperationsMapOptions.cs
//
// PURPOSE:
// Where the map pictures come from (Slice 12C, owner H-5), section "OperationsMaps".
//
// RESPONSIBILITIES:
// - The tile address with {z}, {x}, {y} (and an API key in it when the provider needs one), the attribution and the name sent to the provider
//
// IMPORTANT NOTES:
// - Empty TileUrl: no map; the pages show the stops left in words only
// - The tile address (and its key) stays on the server: browsers ask Vixly for tiles (/maps/tiles), never the provider
// - OpenStreetMap's own tiles are fine for staging, not for production traffic: production needs a provider account
// =========================================================
public sealed class OperationsMapOptions
{
    public const string SectionName = "OperationsMaps";

    public string TileUrl { get; set; } = string.Empty;

    public string Attribution { get; set; } = string.Empty;

    public string UserAgent { get; set; } = "Operations map tiles";

    public int MaxZoom { get; set; } = 18;
}
