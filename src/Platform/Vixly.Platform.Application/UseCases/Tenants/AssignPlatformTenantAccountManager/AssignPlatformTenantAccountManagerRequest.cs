namespace Vixly.Platform.Application.UseCases.Tenants.AssignPlatformTenantAccountManager;

// =========================================================
// FILE: AssignPlatformTenantAccountManagerRequest.cs
//
// PURPOSE:
// Input for AssignPlatformTenantAccountManager.
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Null clears the field
// =========================================================
public sealed class AssignPlatformTenantAccountManagerRequest
{
    public Guid PlatformTenantId { get; init; }

    public Guid? PlatformUserId { get; init; }
}
