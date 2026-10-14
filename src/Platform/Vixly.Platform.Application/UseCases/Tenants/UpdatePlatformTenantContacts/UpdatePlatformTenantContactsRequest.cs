namespace Vixly.Platform.Application.UseCases.Tenants.UpdatePlatformTenantContacts;

// =========================================================
// FILE: UpdatePlatformTenantContactsRequest.cs
//
// PURPOSE:
// Input for UpdatePlatformTenantContacts.
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the domain
// =========================================================
public sealed class UpdatePlatformTenantContactsRequest
{
    public Guid PlatformTenantId { get; init; }

    public IReadOnlyList<PlatformContactInput> Contacts { get; init; } = Array.Empty<PlatformContactInput>();
}
