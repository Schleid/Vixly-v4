namespace Vixly.Operations.Infrastructure.Platform;

// =========================================================
// FILE: OperationsLinkOptions.cs
//
// PURPOSE:
// How links to a tenant portal are built (section OperationsLinks).
//
// RESPONSIBILITIES:
// - Hold the portal address template
//
// IMPORTANT NOTES:
// - {slug} is replaced by the tenant slug; Development uses https://localhost:7404
// =========================================================
public sealed class OperationsLinkOptions
{
    public const string SectionName = "OperationsLinks";

    public string PortalUrlTemplate { get; set; } = "https://{slug}.techcrafters.ltd";
}
