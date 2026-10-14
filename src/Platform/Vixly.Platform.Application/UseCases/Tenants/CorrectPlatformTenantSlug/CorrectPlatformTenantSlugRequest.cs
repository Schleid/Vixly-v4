namespace Vixly.Platform.Application.UseCases.Tenants.CorrectPlatformTenantSlug;

// =========================================================
// FILE: CorrectPlatformTenantSlugRequest.cs
//
// PURPOSE:
// Input for CorrectPlatformTenantSlug.
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - A reason is required
// =========================================================
public sealed class CorrectPlatformTenantSlugRequest
{
    public Guid PlatformTenantId { get; init; }

    public string Slug { get; init; } = string.Empty;

    public string Reason { get; init; } = string.Empty;
}
