namespace Vixly.Platform.Application.UseCases.Tenants.UpdatePlatformTenantSettings;

// =========================================================
// FILE: UpdatePlatformTenantSettingsRequest.cs
//
// PURPOSE:
// Input for UpdatePlatformTenantSettings.
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the domain
// =========================================================
public sealed class UpdatePlatformTenantSettingsRequest
{
    public Guid PlatformTenantId { get; init; }

    public string Language { get; init; } = "en";

    public string? TimeZone { get; init; }
}
