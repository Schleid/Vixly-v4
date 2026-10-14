namespace Vixly.Platform.Application.UseCases.Tenants.SetPlatformTenantTags;

// =========================================================
// FILE: SetPlatformTenantTagsRequest.cs
//
// PURPOSE:
// Input for SetPlatformTenantTags.
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the domain
// =========================================================
public sealed class SetPlatformTenantTagsRequest
{
    public Guid PlatformTenantId { get; init; }

    public IReadOnlyList<string> Tags { get; init; } = Array.Empty<string>();
}
