namespace Vixly.Platform.Domain.Tenants;

// =========================================================
// FILE: PlatformTenantChangeLimits.cs
//
// PURPOSE:
// Size limits of company change requests (B12).
//
// RESPONSIBILITIES:
// - Bound every text and the proof document
//
// IMPORTANT NOTES:
// - The proof is at most 10 MB: a scan or photo of an official document
// =========================================================
public static class PlatformTenantChangeLimits
{
    public const int ReasonMaxLength = 1000;

    public const int NoteMaxLength = 1000;

    public const int ActorMaxLength = 200;

    public const int FileNameMaxLength = 200;

    public const int ContentTypeMaxLength = 50;

    public const int ProofMaxBytes = 10 * 1024 * 1024;

    public const int HashLength = 64;
}
