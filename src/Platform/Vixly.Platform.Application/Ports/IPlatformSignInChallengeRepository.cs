using Vixly.Platform.Domain.Identity;

namespace Vixly.Platform.Application.Ports;

// =========================================================
// FILE: IPlatformSignInChallengeRepository.cs
//
// PURPOSE:
// Loads and adds pending sign-ins.
//
// RESPONSIBILITIES:
// - Find a challenge by token hash
// - Add a new challenge
//
// IMPORTANT NOTES:
// - Expiry is checked by the domain, not the query
// =========================================================
public interface IPlatformSignInChallengeRepository
{
    Task<PlatformSignInChallenge?> FindByTokenHashAsync(string tokenHash, CancellationToken cancellationToken);

    void Add(PlatformSignInChallenge challenge);
}
