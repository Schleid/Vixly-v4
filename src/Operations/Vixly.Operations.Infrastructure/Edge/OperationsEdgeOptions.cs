namespace Vixly.Operations.Infrastructure.Edge;

// =========================================================
// FILE: OperationsEdgeOptions.cs
//
// PURPOSE:
// Where Operations Web sends company brands for the Cloudflare Worker (section Edge; owner, 2026-10-28).
//
// RESPONSIBILITIES:
// - The Worker's address and the shared secret
//
// IMPORTANT NOTES:
// - The secret is an environment variable (Edge__BrandSecret), never in a settings file (owner, 2026-10-23)
// - The same value is the Worker's EDGE_BRAND_SECRET
// - Empty address or secret: nothing is sent (staging and development have no Worker)
// =========================================================
public sealed class OperationsEdgeOptions
{
    public const string SectionName = "Edge";

    public string BrandUrl { get; set; } = string.Empty;

    public string BrandSecret { get; set; } = string.Empty;
}
