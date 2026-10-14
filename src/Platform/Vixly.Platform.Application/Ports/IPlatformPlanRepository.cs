using Vixly.Platform.Domain.Subscriptions;

namespace Vixly.Platform.Application.Ports;

// =========================================================
// FILE: IPlatformPlanRepository.cs
//
// PURPOSE:
// Loads and stores plans with their translations and versions.
//
// RESPONSIBILITIES:
// - Find a plan by id or code, with all its versions
// - Find the plans open to the public catalogue
// - Add a plan
//
// IMPORTANT NOTES:
// - Implemented in Infrastructure
// =========================================================
public interface IPlatformPlanRepository
{
    Task<PlatformPlan?> GetAsync(Guid platformPlanId, CancellationToken cancellationToken);

    Task<PlatformPlan?> GetByCodeAsync(string code, CancellationToken cancellationToken);

    Task<bool> CodeExistsAsync(string code, CancellationToken cancellationToken);

    Task<IReadOnlyList<PlatformPlan>> ListChoosableAsync(bool publicOnly, CancellationToken cancellationToken);

    void Add(PlatformPlan plan);
}
