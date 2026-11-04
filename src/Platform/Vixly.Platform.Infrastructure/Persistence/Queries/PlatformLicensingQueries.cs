using Microsoft.EntityFrameworkCore;
using Vixly.Platform.Application.Ports;
using Vixly.Platform.Application.Queries;
using Vixly.Platform.Domain.Licensing;
using Vixly.Shared.Kernel.Paging;

namespace Vixly.Platform.Infrastructure.Persistence.Queries;

// =========================================================
// FILE: PlatformLicensingQueries.cs
//
// PURPOSE:
// Read models for the licensing screens (D-L15).
//
// RESPONSIBILITIES:
// - Page devices with filters and tenant names
// - Count a tenant's devices by state in one grouped query
// - Read history with staff names
//
// IMPORTANT NOTES:
// - No tracking; idle = not seen for 60 days
// =========================================================
internal sealed class PlatformLicensingQueries : IPlatformLicensingQueries
{
    private static readonly TimeSpan IdleAfter = TimeSpan.FromDays(60);

    private readonly PlatformDbContext _context;

    public PlatformLicensingQueries(PlatformDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public async Task<PagedResult<PlatformDeviceItem>> ListDevicesAsync(PlatformDeviceFilter filter, PagedRequest page, DateTimeOffset utcNow, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(filter);
        ArgumentNullException.ThrowIfNull(page);

        var query =
            from device in _context.Devices.AsNoTracking()
            join tenant in _context.Tenants.AsNoTracking() on device.PlatformTenantId equals tenant.Id
            select new { device, Name = tenant.Company.TradingName ?? tenant.Company.LegalName };

        if (filter.PlatformTenantId is Guid tenantId)
        {
            query = query.Where(row => row.device.PlatformTenantId == tenantId);
        }

        if (filter.Status is { } status)
        {
            query = query.Where(row => row.device.Status == status);
        }

        if (filter.AwaitingReviewOnly)
        {
            query = query.Where(row => row.device.Status == PlatformDeviceStatus.ReleasePending || (row.device.Status == PlatformDeviceStatus.Revoked && row.device.SeatHeld));
        }

        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            string search = filter.Search;
            query = query.Where(row => row.device.Name.Contains(search) || row.device.Number.Contains(search) || row.Name.Contains(search));
        }

        int total = await query.CountAsync(cancellationToken);
        var ordered = filter.AwaitingReviewOnly
            ? query.OrderBy(row => row.device.ReleaseRequestedUtc ?? row.device.EndedUtc)
            : query.OrderBy(row => row.device.Status).ThenBy(row => row.device.Number);
        var rows = await ordered.Skip(page.Skip).Take(page.PageSize).ToListAsync(cancellationToken);

        return new PagedResult<PlatformDeviceItem>(rows.Select(row => Item(row.device, row.Name, utcNow)).ToArray(), page, total);
    }

    public async Task<PlatformDeviceItem?> GetDeviceAsync(Guid deviceId, DateTimeOffset utcNow, CancellationToken cancellationToken)
    {
        var row = await (
            from device in _context.Devices.AsNoTracking()
            join tenant in _context.Tenants.AsNoTracking() on device.PlatformTenantId equals tenant.Id
            where device.Id == deviceId
            select new { device, Name = tenant.Company.TradingName ?? tenant.Company.LegalName }).FirstOrDefaultAsync(cancellationToken);

        return row is null ? null : Item(row.device, row.Name, utcNow);
    }

    public async Task<IReadOnlyList<PlatformDeviceEventItem>> ListEventsAsync(Guid deviceId, CancellationToken cancellationToken)
    {
        var rows = await (
            from item in _context.DeviceEvents.AsNoTracking()
            where item.PlatformDeviceId == deviceId
            join user in _context.Users.AsNoTracking() on item.ActorUserId equals user.Id into users
            from user in users.DefaultIfEmpty()
            orderby item.CreatedUtc descending
            select new { item.CreatedUtc, item.EventCode, item.Reason, item.Note, item.Arguments, item.ActorName, Staff = user == null ? null : user.FirstName + " " + user.LastName })
            .Take(200)
            .ToListAsync(cancellationToken);

        return rows.Select(row => new PlatformDeviceEventItem(row.CreatedUtc, row.EventCode, row.Reason, row.Note, row.Arguments, row.Staff ?? row.ActorName)).ToArray();
    }

    public async Task<PlatformLicensingCounts> CountAsync(Guid platformTenantId, DateTimeOffset utcNow, CancellationToken cancellationToken)
    {
        DateTimeOffset idleSince = utcNow - IdleAfter;
        var counts = await _context.Devices.AsNoTracking()
            .Where(item => item.PlatformTenantId == platformTenantId)
            .GroupBy(item => 1)
            .Select(group => new
            {
                Holding = group.Count(item => item.Status == PlatformDeviceStatus.Active || item.Status == PlatformDeviceStatus.ReleasePending || (item.Status == PlatformDeviceStatus.Revoked && item.SeatHeld)),
                Pending = group.Count(item => item.Status == PlatformDeviceStatus.Pending),
                Review = group.Count(item => item.Status == PlatformDeviceStatus.ReleasePending || (item.Status == PlatformDeviceStatus.Revoked && item.SeatHeld)),
                Idle = group.Count(item => item.Status == PlatformDeviceStatus.Active && (item.LastSeenUtc ?? item.ApprovedUtc) < idleSince)
            })
            .FirstOrDefaultAsync(cancellationToken);

        return counts is null ? new PlatformLicensingCounts(0, 0, 0, 0) : new PlatformLicensingCounts(counts.Holding, counts.Pending, counts.Review, counts.Idle);
    }

    private static PlatformDeviceItem Item(PlatformDevice device, string tenantName, DateTimeOffset utcNow)
    {
        return new PlatformDeviceItem(
            device.Id, device.PlatformTenantId, tenantName, device.Number, device.Name, device.Kind, device.Proof, device.Status, device.SeatHeld, device.Reason,
            device.RequestedByName, device.CreatedUtc, device.ApprovedUtc, device.ReleaseRequestedUtc, device.EndedUtc, device.LastSeenUtc,
            device.Status == PlatformDeviceStatus.Active && (device.LastSeenUtc ?? device.ApprovedUtc ?? device.CreatedUtc) < utcNow - IdleAfter)
        {
            RegisteredIp = device.RegisteredIp,
            RegisteredUserAgent = device.RegisteredUserAgent,
            RegisteredCountry = device.RegisteredCountry,
            ApprovedByName = device.ApprovedByName
        };
    }
}
