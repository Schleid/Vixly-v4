namespace Vixly.Operations.Application.Branding;

// =========================================================
// FILE: OperationsEdgeBrandChanged.cs
//
// PURPOSE:
// The outbox payload after a company published its website: its brand goes to the Cloudflare Worker.
//
// RESPONSIBILITIES:
// - Name the published version, for the record
//
// IMPORTANT NOTES:
// - The handler reads the published brand when it runs, so a retry sends the current one
// =========================================================
public sealed record OperationsEdgeBrandChanged(int PublishedVersion)
{
    public const string MessageType = "ops.edge.brand_changed.v1";
}
