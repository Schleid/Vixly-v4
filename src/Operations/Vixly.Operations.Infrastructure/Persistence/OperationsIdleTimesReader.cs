using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Vixly.Operations.Application.Identity;
using Vixly.Operations.Application.Ports;

namespace Vixly.Operations.Infrastructure.Persistence;

// =========================================================
// FILE: OperationsIdleTimesReader.cs
//
// PURPOSE:
// Reads the company's inactivity times from its settings, kept one minute per company (owner, 2026-10-27).
//
// RESPONSIBILITIES:
// - Read the three numbers without tracking the settings row
// - Keep them per tenant for one minute; forget them on a change
//
// IMPORTANT NOTES:
// - Outside a tenant (system work), the defaults
// =========================================================
internal sealed class OperationsIdleTimesReader : IOperationsIdleTimesReader
{
    private static readonly TimeSpan KeptFor = TimeSpan.FromMinutes(1);

    private readonly OperationsDbContext _context;

    private readonly IOperationsTenantScope _scope;

    private readonly IMemoryCache _cache;

    public OperationsIdleTimesReader(OperationsDbContext context, IOperationsTenantScope scope, IMemoryCache cache)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _scope = scope ?? throw new ArgumentNullException(nameof(scope));
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
    }

    public async Task<OperationsIdleTimes> GetAsync(CancellationToken cancellationToken)
    {
        if (_scope.TenantId is not { } tenantId)
        {
            return OperationsIdleTimes.Default;
        }

        if (_cache.TryGetValue(Key(tenantId), out OperationsIdleTimes? kept) && kept is not null)
        {
            return kept;
        }

        var row = await _context.TenantSettings.AsNoTracking()
            .Select(settings => new { settings.StaffIdleMinutes, settings.CustomerIdleMinutes, settings.IdleWarnMinutes })
            .FirstOrDefaultAsync(cancellationToken);
        OperationsIdleTimes times = row is null
            ? OperationsIdleTimes.Default
            : new OperationsIdleTimes(TimeSpan.FromMinutes(row.StaffIdleMinutes), TimeSpan.FromMinutes(row.CustomerIdleMinutes), TimeSpan.FromMinutes(row.IdleWarnMinutes));
        _cache.Set(Key(tenantId), times, KeptFor);
        return times;
    }

    public void Forget()
    {
        if (_scope.TenantId is { } tenantId)
        {
            _cache.Remove(Key(tenantId));
        }
    }

    private static string Key(Guid tenantId) => "vixly.ops.idle." + tenantId.ToString("N");
}
