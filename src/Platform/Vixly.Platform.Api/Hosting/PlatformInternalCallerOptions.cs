namespace Vixly.Platform.Api.Hosting;

// =========================================================
// FILE: PlatformInternalCallerOptions.cs
//
// PURPOSE:
// One plane allowed to call Platform: its scopes and public keys (Slice 9, D-K1).
//
// RESPONSIBILITIES:
// - Carry the scopes and key ids of one caller
//
// IMPORTANT NOTES:
// - Scopes: portal, tenant, events, jobs
// =========================================================
public sealed class PlatformInternalCallerOptions
{
    public string[] Scopes { get; set; } = Array.Empty<string>();

    public Dictionary<string, string> Keys { get; set; } = new(StringComparer.Ordinal);
}
