using Vixly.Platform.Domain.Identity;

namespace Vixly.Platform.Application.Ports;

// =========================================================
// FILE: IPlatformSecuritySettingsRepository.cs
//
// PURPOSE:
// Storage of Control's sign-in rule (Require MFA).
//
// RESPONSIBILITIES:
// - Read the one row, or nothing (which means Yes)
// - Add it the first time it is changed
//
// IMPORTANT NOTES:
// - Read at every sign-in; one small row
// =========================================================
public interface IPlatformSecuritySettingsRepository
{
    Task<PlatformSecuritySettings?> GetAsync(CancellationToken cancellationToken);

    void Add(PlatformSecuritySettings settings);
}
