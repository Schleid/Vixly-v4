namespace Vixly.Platform.Infrastructure.Edge;

// =========================================================
// FILE: PlatformEdgeOptions.cs
//
// PURPOSE:
// Where Control sends the maintenance windows for the Cloudflare Worker (section Edge; owner, 2026-10-28).
//
// RESPONSIBILITIES:
// - The Worker's address and the shared secret
//
// IMPORTANT NOTES:
// - The secret is an environment variable (Edge__MaintenanceSecret), never in a settings file (owner, 2026-10-23)
// - The same value is the Worker's EDGE_MAINTENANCE_SECRET
// - Empty address or secret: nothing is sent (staging and development have no Worker)
// =========================================================
public sealed class PlatformEdgeOptions
{
    public const string SectionName = "Edge";

    public string MaintenanceUrl { get; set; } = string.Empty;

    public string MaintenanceSecret { get; set; } = string.Empty;
}
