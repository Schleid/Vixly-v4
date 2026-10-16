namespace Vixly.Platform.Application.UseCases.Collections.CheckPlatformTenantCollections;

// =========================================================
// FILE: CheckPlatformTenantCollectionsRequest.cs
//
// PURPOSE:
// Looks at one tenant again now, as the clock would (D-C8).
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class CheckPlatformTenantCollectionsRequest
{
    public Guid PlatformTenantId { get; init; }
}
