using Vixly.Platform.Domain.Licensing;

namespace Vixly.Platform.Application.Ports;

// =========================================================
// FILE: IPlatformLicensingRepository.cs
//
// PURPOSE:
// Loads and stores seats, devices, challenges, links and licensing history.
//
// RESPONSIBILITIES:
// - Find a tenant's seat row and devices
// - Count pending devices and tenant releases in the allowance window
// - Find challenges and links by hash
//
// IMPORTANT NOTES:
// - Writes are committed by the unit of work
// - History rows are append-only
// =========================================================
public interface IPlatformLicensingRepository
{
    Task<PlatformTenantLicensing?> GetSeatsAsync(Guid platformTenantId, CancellationToken cancellationToken);

    void AddSeats(PlatformTenantLicensing seats);

    Task<PlatformDevice?> GetDeviceAsync(Guid deviceId, CancellationToken cancellationToken);

    void AddDevice(PlatformDevice device);

    void AddEvents(IEnumerable<PlatformDeviceEvent> events);

    Task<int> CountPendingAsync(Guid platformTenantId, CancellationToken cancellationToken);

    Task<int> CountEndedByTenantSinceAsync(Guid platformTenantId, DateTimeOffset sinceUtc, CancellationToken cancellationToken);

    Task<int> CountHoldingSeatsAsync(Guid platformTenantId, CancellationToken cancellationToken);

    Task<IReadOnlyList<PlatformDevice>> ListOpenDevicesAsync(Guid platformTenantId, CancellationToken cancellationToken);

    Task<IReadOnlyList<Guid>> ListExpiredPendingAsync(DateTimeOffset createdBeforeUtc, int take, CancellationToken cancellationToken);

    void AddChallenge(PlatformDeviceChallenge challenge);

    Task<PlatformDeviceChallenge?> GetChallengeAsync(string challengeHash, CancellationToken cancellationToken);

    void AddLink(PlatformDeviceLink link);

    Task<PlatformDeviceLink?> GetLinkAsync(string tokenHash, CancellationToken cancellationToken);

    Task<IReadOnlyList<PlatformDeviceLink>> ListOpenLinksAsync(Guid platformTenantId, PlatformDeviceLinkKind kind, CancellationToken cancellationToken);

    Task<int> CountLinkRequestsSinceAsync(Guid platformTenantId, DateTimeOffset sinceUtc, CancellationToken cancellationToken);
}
