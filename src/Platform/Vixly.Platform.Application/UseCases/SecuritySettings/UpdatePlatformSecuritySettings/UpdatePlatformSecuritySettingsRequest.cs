namespace Vixly.Platform.Application.UseCases.SecuritySettings.UpdatePlatformSecuritySettings;

// =========================================================
// FILE: UpdatePlatformSecuritySettingsRequest.cs
//
// PURPOSE:
// Input for UpdatePlatformSecuritySettings: Require MFA Yes or No.
//
// RESPONSIBILITIES:
// - Carry the values named above, nothing more
//
// IMPORTANT NOTES:
// - Part of Require MFA (owner, 2026-10-22)
// =========================================================
public sealed class UpdatePlatformSecuritySettingsRequest
{
    public bool RequireMfa { get; init; } = true;
}
