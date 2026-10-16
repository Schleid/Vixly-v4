using Microsoft.EntityFrameworkCore;
using Vixly.Platform.Application.Ports;
using Vixly.Platform.Domain.Licensing;

namespace Vixly.Platform.Infrastructure.Persistence.Repositories;

// =========================================================
// FILE: PlatformLicensingRepository.cs
//
// PURPOSE:
// Stores seats, devices, challenges, links and licensing history.
//
// RESPONSIBILITIES:
// - Look in rows of the current transaction first where a commit adds and reads
// - Count pending devices, tenant releases in the window and link requests
//
// IMPORTANT NOTES:
// - History rows are only added
// =========================================================
internal sealed class PlatformLicensingRepository : IPlatformLicensingRepository
{
    private readonly PlatformDbContext _context;

    public PlatformLicensingRepository(PlatformDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public async Task<PlatformTenantLicensing?> GetSeatsAsync(Guid platformTenantId, CancellationToken cancellationToken)
    {
        return _context.TenantLicensing.Local.FirstOrDefault(item => item.PlatformTenantId == platformTenantId)
            ?? await _context.TenantLicensing.FirstOrDefaultAsync(item => item.PlatformTenantId == platformTenantId, cancellationToken);
    }

    public void AddSeats(PlatformTenantLicensing seats) => _context.TenantLicensing.Add(seats);

    public async Task<PlatformDevice?> GetDeviceAsync(Guid deviceId, CancellationToken cancellationToken)
    {
        return _context.Devices.Local.FirstOrDefault(item => item.Id == deviceId)
            ?? await _context.Devices.FirstOrDefaultAsync(item => item.Id == deviceId, cancellationToken);
    }

    public void AddDevice(PlatformDevice device) => _context.Devices.Add(device);

    public void AddEvents(IEnumerable<PlatformDeviceEvent> events) => _context.DeviceEvents.AddRange(events);

    public Task<int> CountPendingAsync(Guid platformTenantId, CancellationToken cancellationToken)
    {
        return _context.Devices.AsNoTracking().CountAsync(item => item.PlatformTenantId == platformTenantId && item.Status == PlatformDeviceStatus.Pending, cancellationToken);
    }

    public Task<int> CountEndedByTenantSinceAsync(Guid platformTenantId, DateTimeOffset sinceUtc, CancellationToken cancellationToken)
    {
        return _context.Devices.AsNoTracking().CountAsync(
            item => item.PlatformTenantId == platformTenantId && item.EndedByTenant && item.EndedUtc >= sinceUtc,
            cancellationToken);
    }

    public Task<int> CountHoldingSeatsAsync(Guid platformTenantId, CancellationToken cancellationToken)
    {
        return _context.Devices.AsNoTracking().CountAsync(
            item => item.PlatformTenantId == platformTenantId
                && (item.Status == PlatformDeviceStatus.Active || item.Status == PlatformDeviceStatus.ReleasePending || (item.Status == PlatformDeviceStatus.Revoked && item.SeatHeld)),
            cancellationToken);
    }

    public async Task<IReadOnlyList<PlatformDevice>> ListOpenDevicesAsync(Guid platformTenantId, CancellationToken cancellationToken)
    {
        return await _context.Devices
            .Where(item => item.PlatformTenantId == platformTenantId
                && (item.Status == PlatformDeviceStatus.Pending || item.Status == PlatformDeviceStatus.Active || item.Status == PlatformDeviceStatus.ReleasePending
                    || (item.Status == PlatformDeviceStatus.Revoked && item.SeatHeld)))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Guid>> ListExpiredPendingAsync(DateTimeOffset createdBeforeUtc, int take, CancellationToken cancellationToken)
    {
        return await _context.Devices.AsNoTracking()
            .Where(item => item.Status == PlatformDeviceStatus.Pending && item.CreatedUtc <= createdBeforeUtc)
            .OrderBy(item => item.CreatedUtc)
            .Select(item => item.Id)
            .Take(take)
            .ToListAsync(cancellationToken);
    }

    public void AddChallenge(PlatformDeviceChallenge challenge) => _context.DeviceChallenges.Add(challenge);

    public Task<PlatformDeviceChallenge?> GetChallengeAsync(string challengeHash, CancellationToken cancellationToken)
    {
        return _context.DeviceChallenges.FirstOrDefaultAsync(item => item.ChallengeHash == challengeHash, cancellationToken);
    }

    public void AddLink(PlatformDeviceLink link) => _context.DeviceLinks.Add(link);

    public Task<PlatformDeviceLink?> GetLinkAsync(string tokenHash, CancellationToken cancellationToken)
    {
        return _context.DeviceLinks.FirstOrDefaultAsync(item => item.TokenHash == tokenHash, cancellationToken);
    }

    public async Task<IReadOnlyList<PlatformDeviceLink>> ListOpenLinksAsync(Guid platformTenantId, PlatformDeviceLinkKind kind, CancellationToken cancellationToken)
    {
        return await _context.DeviceLinks
            .Where(item => item.PlatformTenantId == platformTenantId && item.Kind == kind && item.UsedUtc == null && item.VoidedUtc == null)
            .ToListAsync(cancellationToken);
    }

    public Task<int> CountLinkRequestsSinceAsync(Guid platformTenantId, DateTimeOffset sinceUtc, CancellationToken cancellationToken)
    {
        return _context.DeviceEvents.AsNoTracking().CountAsync(
            item => item.PlatformTenantId == platformTenantId && item.EventCode == PlatformDeviceEventCodes.LinkSent && item.CreatedUtc >= sinceUtc,
            cancellationToken);
    }
}
