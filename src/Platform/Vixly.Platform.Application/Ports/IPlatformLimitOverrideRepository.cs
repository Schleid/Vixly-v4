using Vixly.Platform.Domain.Subscriptions;

namespace Vixly.Platform.Application.Ports;

// =========================================================
// FILE: IPlatformLimitOverrideRepository.cs
//
// PURPOSE:
// Loads and stores a tenant's limit overrides.
//
// RESPONSIBILITIES:
// - List the open overrides of a tenant
// - Find one override
// - Add an override
//
// IMPORTANT NOTES:
// - Implemented in Infrastructure; removed overrides stay as history (D-S8)
// =========================================================
public interface IPlatformLimitOverrideRepository
{
    Task<IReadOnlyList<PlatformTenantLimitOverride>> ListOpenAsync(Guid platformTenantId, CancellationToken cancellationToken);

    Task<PlatformTenantLimitOverride?> GetAsync(Guid overrideId, CancellationToken cancellationToken);

    void Add(PlatformTenantLimitOverride limitOverride);
}
