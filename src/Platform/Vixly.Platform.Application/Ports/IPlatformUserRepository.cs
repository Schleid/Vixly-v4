using Vixly.Platform.Domain.Identity;
using Vixly.Shared.Kernel.ValueObjects;

namespace Vixly.Platform.Application.Ports;

// =========================================================
// FILE: IPlatformUserRepository.cs
//
// PURPOSE:
// Loads and adds PlatformUser aggregates.
//
// RESPONSIBILITIES:
// - Load a whole user (account, second factors, grants,
//   memberships) for a command
// - Check email uniqueness
// - Find the owner of a passkey by its credential id
// - Count other active SuperUsers for the safety rules
//
// IMPORTANT NOTES:
// - Read screens use query ports with projections (P-1), not
//   this repository
// =========================================================
public interface IPlatformUserRepository
{
    Task<PlatformUser?> GetAsync(Guid platformUserId, CancellationToken cancellationToken);

    // Harmonization H-10: only what the session check reads; null for an unknown staff member.
    Task<Vixly.Platform.Application.Identity.PlatformSessionFacts?> FindSessionFactsAsync(Guid platformUserId, CancellationToken cancellationToken);

    Task<PlatformUser?> FindByEmailAsync(EmailAddress email, CancellationToken cancellationToken);

    Task<bool> EmailExistsAsync(EmailAddress email, CancellationToken cancellationToken);

    Task<PlatformUser?> FindByPasskeyAsync(byte[] credentialId, CancellationToken cancellationToken);

    Task<int> CountOtherActiveSuperUsersAsync(Guid excludedUserId, CancellationToken cancellationToken);

    Task<bool> AnyAsync(CancellationToken cancellationToken);

    // The next employee number (TC-0001 ...), from a database sequence: never repeated, even across failed saves.
    Task<string> NextEmployeeNumberAsync(CancellationToken cancellationToken);

    void Add(PlatformUser user);
}
