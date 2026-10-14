using Vixly.Platform.Domain.Identity;

namespace Vixly.Platform.Application.Ports;

// =========================================================
// FILE: IPlatformPasskeyCeremonyRepository.cs
//
// PURPOSE:
// Loads and adds passkey ceremonies.
//
// RESPONSIBILITIES:
// - Find a ceremony by the hash of its token; add one
//
// IMPORTANT NOTES:
// - Expired ceremonies are removed by the Worker (Slice 4)
// =========================================================
public interface IPlatformPasskeyCeremonyRepository
{
    Task<PlatformPasskeyCeremony?> FindByTokenHashAsync(string tokenHash, CancellationToken cancellationToken);

    void Add(PlatformPasskeyCeremony ceremony);
}
