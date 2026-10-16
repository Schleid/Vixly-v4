using Vixly.Platform.Domain.Collections;

namespace Vixly.Platform.Application.Ports;

// =========================================================
// FILE: IPlatformCollectionsRepository.cs
//
// PURPOSE:
// Loads and stores collections settings, cases and their history.
//
// RESPONSIBILITIES:
// - Find the settings row and a tenant's open case
// - Say whether a card retry was already recorded
// - List the tenants the clock should look at
//
// IMPORTANT NOTES:
// - Writes are committed by the unit of work
// - History rows are append-only
// =========================================================
public interface IPlatformCollectionsRepository
{
    Task<PlatformCollectionsSettings?> FindSettingsAsync(CancellationToken cancellationToken);

    void AddSettings(PlatformCollectionsSettings settings);

    Task<PlatformCollectionCase?> GetOpenCaseAsync(Guid platformTenantId, CancellationToken cancellationToken);

    Task<PlatformCollectionCase?> GetCaseAsync(Guid caseId, CancellationToken cancellationToken);

    void AddCase(PlatformCollectionCase collectionCase);

    void AddEvents(IEnumerable<PlatformCollectionCaseEvent> events);

    Task<bool> RetryRecordedAsync(string retryKey, CancellationToken cancellationToken);

    // Tenants with an overdue invoice, an open case or a Pending payment subscription; ordered by id, after the given one.
    Task<IReadOnlyList<Guid>> ListTenantsToCheckAsync(DateTimeOffset utcNow, Guid? after, int take, CancellationToken cancellationToken);
}
