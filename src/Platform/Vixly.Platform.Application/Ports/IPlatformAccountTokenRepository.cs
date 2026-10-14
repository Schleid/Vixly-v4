using Vixly.Platform.Domain.Identity;

namespace Vixly.Platform.Application.Ports;

// =========================================================
// FILE: IPlatformAccountTokenRepository.cs
//
// PURPOSE:
// Loads and adds invitation and password-reset links.
//
// RESPONSIBILITIES:
// - Find a link by token hash
// - List a user's unused links of one purpose, to revoke
//   them when a newer one is issued
//
// IMPORTANT NOTES:
// - Unused means neither used nor revoked
// =========================================================
public interface IPlatformAccountTokenRepository
{
    Task<PlatformAccountToken?> FindByTokenHashAsync(string tokenHash, CancellationToken cancellationToken);

    Task<IReadOnlyList<PlatformAccountToken>> ListUnusedAsync(
        Guid platformUserId,
        PlatformAccountTokenPurpose purpose,
        CancellationToken cancellationToken);

    void Add(PlatformAccountToken token);
}
