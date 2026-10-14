using Vixly.Platform.Domain.Identity;

namespace Vixly.Platform.Application.Ports;

// =========================================================
// FILE: IPlatformSessionRepository.cs
//
// PURPOSE:
// Loads and adds staff sessions.
//
// RESPONSIBILITIES:
// - Find a session by id or by token hash
// - List a user's open sessions, to end them together
//
// IMPORTANT NOTES:
// - Open means not ended; expiry is checked by the domain
// =========================================================
public interface IPlatformSessionRepository
{
    Task<PlatformUserSession?> GetAsync(Guid sessionId, CancellationToken cancellationToken);

    Task<PlatformUserSession?> FindByTokenHashAsync(string tokenHash, CancellationToken cancellationToken);

    Task<IReadOnlyList<PlatformUserSession>> ListOpenAsync(Guid platformUserId, CancellationToken cancellationToken);

    void Add(PlatformUserSession session);
}
