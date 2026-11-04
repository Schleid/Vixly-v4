using System.Globalization;
using Vixly.Operations.Application.Ports;
using Vixly.Operations.Domain.Settings;

namespace Vixly.Operations.Application.Settings;

// =========================================================
// FILE: OperationsNumbering.cs
//
// PURPOSE:
// Issues the tenant's document numbers: prefix, two-digit year, six digits (D-O12).
//
// RESPONSIBILITIES:
// - Read the prefix from the settings and take the next number of the year
//
// IMPORTANT NOTES:
// - Used from Slice 3 on (shipments, receiving, invoices, payments)
// - The year is the UTC year
// =========================================================
public sealed class OperationsNumbering
{
    private readonly IOperationsNumberAllocator _allocator;

    private readonly IOperationsSettingsRepository _settings;

    private readonly IOperationsTenantScope _scope;

    private readonly TimeProvider _timeProvider;

    public OperationsNumbering(IOperationsNumberAllocator allocator, IOperationsSettingsRepository settings, IOperationsTenantScope scope, TimeProvider timeProvider)
    {
        _allocator = allocator ?? throw new ArgumentNullException(nameof(allocator));
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        _scope = scope ?? throw new ArgumentNullException(nameof(scope));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public async Task<string> NextAsync(OperationsNumberKind kind, CancellationToken cancellationToken)
    {
        Guid tenantId = _scope.TenantId ?? throw new InvalidOperationException("Numbers are issued inside a tenant.");
        DateTimeOffset utcNow = _timeProvider.GetUtcNow();
        OperationsTenantSettings settings = await _settings.FindAsync(cancellationToken) ?? OperationsTenantSettings.CreateDefault(tenantId, utcNow);
        long number = await _allocator.NextAsync(kind, utcNow.Year, cancellationToken);

        return Format(settings.Prefix(kind), utcNow.Year, number);
    }

    // Customer codes have no year: one sequence for all years, at least five digits (D-C1).
    public async Task<string> NextCustomerCodeAsync(CancellationToken cancellationToken)
    {
        Guid tenantId = _scope.TenantId ?? throw new InvalidOperationException("Numbers are issued inside a tenant.");
        OperationsTenantSettings settings = await _settings.FindAsync(cancellationToken) ?? OperationsTenantSettings.CreateDefault(tenantId, _timeProvider.GetUtcNow());
        long number = await _allocator.NextAsync(OperationsNumberKind.Customer, 0, cancellationToken);

        return settings.Prefix(OperationsNumberKind.Customer) + number.ToString("00000", CultureInfo.InvariantCulture);
    }

    public static string Format(string prefix, int year, long number) =>
        prefix + (year % 100).ToString("00", CultureInfo.InvariantCulture) + number.ToString("000000", CultureInfo.InvariantCulture);
}
