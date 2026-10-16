using Vixly.Platform.Domain.Tenants;

namespace Vixly.Platform.Application.Ports;

// =========================================================
// FILE: IPlatformTenantRegistrationRepository.cs
//
// PURPOSE:
// Loads and stores self-service registrations and their
// verification links.
//
// RESPONSIBILITIES:
// - Find a registration by id or by the hash of its access token
// - Find a verification link by hash; list unused links
//
// IMPORTANT NOTES:
// - Only hashes are looked up: tokens are never stored
// =========================================================
public interface IPlatformTenantRegistrationRepository
{
    Task<PlatformTenantRegistration?> GetAsync(Guid registrationId, CancellationToken cancellationToken);

    Task<PlatformTenantRegistration?> FindByAccessHashAsync(string accessTokenHash, CancellationToken cancellationToken);

    Task<PlatformTenantRegistration?> FindByTenantAsync(Guid platformTenantId, CancellationToken cancellationToken);

    Task<PlatformRegistrationToken?> FindTokenByHashAsync(string tokenHash, CancellationToken cancellationToken);

    Task<IReadOnlyList<PlatformRegistrationToken>> ListUnusedTokensAsync(Guid registrationId, CancellationToken cancellationToken);

    void Add(PlatformTenantRegistration registration);

    void AddToken(PlatformRegistrationToken token);
}
