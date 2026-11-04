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

    // Operations only (Stage 3, Slice 0): a tenant still being set up (Draft or pending activation)
    // answers its status instead of "not found", so Operations can serve the owner's setup pages. Set by the endpoint from
    // the caller's name, never from the request.
    public bool IncludeSettingUp { get; init; }
}
