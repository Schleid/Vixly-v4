using Vixly.Operations.Application.Ports;
using Vixly.Operations.Domain.Locations;
using Vixly.Operations.Domain.Settings;
using Vixly.Operations.Domain.Shipments;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.Plans;

// =========================================================
// FILE: OperationsShipmentCap.cs
//
// PURPOSE:
// Applies the plan's monthly shipment cap (shipment cap, owner 2026-10-29).
//
// RESPONSIBILITIES:
// - Count the company's shipments this calendar month, in its time zone
// - Say whether a new shipment may be created: above 150 percent of the cap it may not, except in the one-time grace month
// - Use the grace month the first time a company goes above 150 percent (once per company, owner C-6)
//
// IMPORTANT NOTES:
// - Only shipments created at the counter and in the portal ask; receiving never does (packages physically arrive)
// - No cap (or Platform not reachable): nothing is refused
// - Every shipment above the cap is charged by Platform when the month closes (owner C-4), the grace month included
// =========================================================
public sealed class OperationsShipmentCap
{
    public static readonly int[] Levels = { 80, 100, 125, 150 };

    private readonly IOperationsPlanFeatures _features;

    private readonly IOperationsShipmentCounter _counter;

    private readonly IOperationsSettingsRepository _settings;

    private readonly IOperationsUnitOfWork _unitOfWork;

    public OperationsShipmentCap(IOperationsPlanFeatures features, IOperationsShipmentCounter counter, IOperationsSettingsRepository settings, IOperationsUnitOfWork unitOfWork)
    {
        _features = features ?? throw new ArgumentNullException(nameof(features));
        _counter = counter ?? throw new ArgumentNullException(nameof(counter));
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
    }

    // 150 percent of the cap, rounded down: the last shipment allowed in a month after the grace month.
    public static int BlockAt(int cap) => cap <= 0 ? int.MaxValue : cap * 3 / 2;

    public static int LevelOf(int used, int cap)
    {
        if (cap <= 0)
        {
            return 0;
        }

        return Levels.LastOrDefault(level => used * 100L >= (long)cap * level);
    }

    // The calendar month of a moment in the company's time zone: its name ("2026-10") and its start and end in UTC.
    public static (string Month, DateTimeOffset StartUtc, DateTimeOffset EndUtc) MonthOf(DateTimeOffset utcNow, string? timeZone, int monthsBack = 0)
    {
        DateTime local = OperationsTimeZones.Local(utcNow, timeZone);
        DateTime first = new DateTime(local.Year, local.Month, 1, 0, 0, 0, DateTimeKind.Unspecified).AddMonths(-monthsBack);
        return (first.ToString("yyyy-MM", System.Globalization.CultureInfo.InvariantCulture), OperationsTimeZones.At(first, timeZone), OperationsTimeZones.At(first.AddMonths(1), timeZone));
    }

    public async Task<OperationsShipmentCapStatus> StatusAsync(Guid tenantId, DateTimeOffset utcNow, CancellationToken cancellationToken, int monthsBack = 0)
    {
        OperationsPlanFeatures features = await _features.GetAsync(tenantId, cancellationToken);
        OperationsTenantSettings? settings = await _settings.FindAsync(cancellationToken);
        (string month, DateTimeOffset start, DateTimeOffset end) = MonthOf(utcNow, settings?.TimeZone, monthsBack);
        int used = await _counter.CountCreatedAsync(start, end, cancellationToken);
        int cap = features.IsKnown ? features.ShipmentCap : 0;

        return new OperationsShipmentCapStatus(
            month,
            used,
            cap,
            features.ExtraShipmentPrice,
            features.CurrencyCode,
            LevelOf(used, cap),
            settings is null || settings.MayUseShipmentGrace(month),
            settings?.ShipmentGraceMonth == month);
    }

    // Null when a new shipment may be created; the error (with the numbers) when the month is full.
    public async Task<Error?> RefuseNewAsync(Guid tenantId, DateTimeOffset utcNow, CancellationToken cancellationToken)
    {
        OperationsShipmentCapStatus status = await StatusAsync(tenantId, utcNow, cancellationToken);

        if (!status.Capped || status.Used < status.BlockAt)
        {
            return null;
        }

        if (status.GraceAvailable)
        {
            if (!status.GraceThisMonth && await _settings.FindAsync(cancellationToken) is { } settings)
            {
                settings.UseShipmentGrace(status.Month, utcNow);
                await _unitOfWork.SaveChangesAsync(cancellationToken);
            }

            return null;
        }

        return OperationsShipmentErrors.PlanLimitReached;
    }
}
