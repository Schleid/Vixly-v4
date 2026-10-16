namespace Vixly.Platform.Application.UseCases.Licensing.GetPlatformPortalAccess;

// =========================================================
// FILE: GetPlatformPortalAccessRequest.cs
//
// PURPOSE:
// Tells Edge and Operations which tenant a portal slug belongs to, its status and access level (D-L10).
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class GetPlatformPortalAccessRequest
{
    public string? Slug { get; init; }
}
