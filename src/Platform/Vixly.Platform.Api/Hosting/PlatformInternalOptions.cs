namespace Vixly.Platform.Api.Hosting;

// =========================================================
// FILE: PlatformInternalOptions.cs
//
// PURPOSE:
// Settings of the internal channel for Operations and Edge (Slice 9, D-K1, D-K2).
//
// RESPONSIBILITIES:
// - Name the internal hostnames and the callers with their public keys and scopes
//
// IMPORTANT NOTES:
// - Section PlatformInternal; off unless both hosts and callers are set
// - Public keys only (base64 SubjectPublicKeyInfo, P-256); private keys stay with each caller
// - Two keys per caller allow rotation
// =========================================================
public sealed class PlatformInternalOptions
{
    public const string SectionName = "PlatformInternal";

    public const string Audience = "vixly-platform-internal";

    public string[] Hosts { get; set; } = Array.Empty<string>();

    public Dictionary<string, PlatformInternalCallerOptions> Callers { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    public bool Enabled => Hosts.Length > 0 && Callers.Count > 0;
}
