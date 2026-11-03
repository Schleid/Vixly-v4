using Vixly.Operations.Application.Messaging;
using Vixly.Operations.Application.Ports;

namespace Vixly.Operations.Application.Plans;

// =========================================================
// FILE: OperationsShipmentCapReport.cs
//
// PURPOSE:
// The hourly round of one company: reports its shipments to Platform and warns the Owner (shipment cap, owner 2026-10-29).
//
// RESPONSIBILITIES:
// - Send this month's count to Platform; in the first 3 days of a month also send last month as final, so Platform charges what was above the cap
// - Warn the Owner once at each level this month: 80, 100 and 125 percent, and 150 percent (the grace month, or the stop)
//
// IMPORTANT NOTES:
// - Every company is counted, capped or not, so Control sees the usage (owner C-3)
// - Platform charges a closed month only once; sending it again is harmless
// =========================================================
public sealed class OperationsShipmentCapReport
{
    private const int FinalDays = 3;

    private readonly OperationsShipmentCap _cap;

    private readonly IOperationsUsageReporter _reporter;

    private readonly IOperationsSettingsRepository _settings;

    private readonly OperationsOutboxWriter _outbox;

    private readonly IOperationsUnitOfWork _unitOfWork;

    public OperationsShipmentCapReport(OperationsShipmentCap cap, IOperationsUsageReporter reporter, IOperationsSettingsRepository settings, OperationsOutboxWriter outbox,
        IOperationsUnitOfWork unitOfWork)
    {
        _cap = cap ?? throw new ArgumentNullException(nameof(cap));
        _reporter = reporter ?? throw new ArgumentNullException(nameof(reporter));
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        _outbox = outbox ?? throw new ArgumentNullException(nameof(outbox));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
    }

    // Returns the number of warnings written.
    public async Task<int> RunAsync(Guid tenantId, DateTimeOffset utcNow, CancellationToken cancellationToken)
    {
        OperationsShipmentCapStatus status = await _cap.StatusAsync(tenantId, utcNow, cancellationToken);
        await _reporter.ReportShipmentsAsync(tenantId, status.Month, status.Used, false, cancellationToken);

        Vixly.Operations.Domain.Settings.OperationsTenantSettings? settings = await _settings.FindAsync(cancellationToken);

        if (Vixly.Operations.Domain.Locations.OperationsTimeZones.Local(utcNow, settings?.TimeZone).Day <= FinalDays)
        {
            OperationsShipmentCapStatus last = await _cap.StatusAsync(tenantId, utcNow, cancellationToken, monthsBack: 1);
            await _reporter.ReportShipmentsAsync(tenantId, last.Month, last.Used, true, cancellationToken);
        }

        if (!status.Capped || status.Level == 0 || settings is null || !settings.RecordShipmentCapNotice(status.Month, status.Level, utcNow))
        {
            return 0;
        }

        _outbox.Write(OperationsShipmentCapNotice.MessageType, tenantId, settings.Id, new OperationsShipmentCapNotice(
            status.Month, status.Level, status.Used, status.Cap, status.BlockAt, status.ExtraShipmentPrice, status.CurrencyCode, status.GraceThisMonth || (status.Level >= 150 && status.GraceAvailable)));
        return await _unitOfWork.SaveChangesAsync(cancellationToken) ? 1 : 0;
    }
}
