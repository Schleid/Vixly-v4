using Vixly.Operations.Domain.Common;
using Vixly.Shared.Kernel.Domain;

namespace Vixly.Operations.Domain.Reports;

// =========================================================
// FILE: OperationsReportSchedule.cs
//
// PURPOSE:
// A report emailed on a schedule to staff of the company (Slice 11; owner, 2026-10-22).
//
// RESPONSIBILITIES:
// - Keep the report, its filters, the frequency, the file format and the staff it goes to
// - Know when it is due and move to the next time once sent
//
// IMPORTANT NOTES:
// - Recipients are staff of the company only (1 to 10); each gets the report as their own rights allow
// - The file is made, emailed and not kept
// =========================================================
public sealed class OperationsReportSchedule : OperationsEntity
{
    public const int MaxRecipients = 10;

    public const int MaxPerCompany = 50;

    public const int KeyMaxLength = 40;

    private readonly List<Guid> _recipients = new();

    private OperationsReportSchedule()
    {
        ReportKey = string.Empty;
    }

    private OperationsReportSchedule(Guid tenantId, DateTimeOffset utcNow)
        : base(tenantId, utcNow)
    {
        ReportKey = string.Empty;
    }

    public string ReportKey { get; private set; }

    public OperationsReportFrequency Frequency { get; private set; }

    public OperationsReportFormat Format { get; private set; }

    public Guid? WarehouseId { get; private set; }

    // The report's cut and rows, as numbers of the application's lists (day/week/month; period/service/...).
    public int Grain { get; private set; }

    public int? Dimension { get; private set; }

    public int Days { get; private set; }

    public IReadOnlyList<Guid> Recipients => _recipients;

    public Guid CreatedByUserId { get; private set; }

    public DateTimeOffset NextRunUtc { get; private set; }

    public DateTimeOffset? LastSentUtc { get; private set; }

    // Rounds in a row in which nothing could be emailed (tried again later, then the period is skipped).
    public int Failures { get; private set; }

    public const int MaxFailures = 8;

    public static OperationsReportSchedule Create(Guid tenantId, string reportKey, OperationsReportFrequency frequency, OperationsReportFormat format, Guid? warehouseId, int grain,
        int? dimension, int days, IReadOnlyCollection<Guid> recipients, Guid by, DateTimeOffset firstRunUtc, DateTimeOffset utcNow)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(reportKey);
        ArgumentNullException.ThrowIfNull(recipients);

        if (reportKey.Length > KeyMaxLength || !Enum.IsDefined(frequency) || !Enum.IsDefined(format))
        {
            throw new DomainException(OperationsReportErrors.NotFound);
        }

        List<Guid> distinct = recipients.Where(id => id != Guid.Empty).Distinct().ToList();

        if (distinct.Count is 0 or > MaxRecipients)
        {
            throw new DomainException(OperationsReportErrors.RecipientsInvalid);
        }

        OperationsReportSchedule schedule = new(tenantId, utcNow)
        {
            ReportKey = reportKey,
            Frequency = frequency,
            Format = format,
            WarehouseId = warehouseId,
            Grain = grain,
            Dimension = dimension,
            Days = days,
            CreatedByUserId = by,
            NextRunUtc = firstRunUtc
        };
        schedule._recipients.AddRange(distinct);
        return schedule;
    }

    public bool Due(DateTimeOffset utcNow) => NextRunUtc <= utcNow;

    public void Sent(DateTimeOffset nextRunUtc, DateTimeOffset utcNow)
    {
        (LastSentUtc, NextRunUtc, Failures) = (utcNow, nextRunUtc, 0);
        MarkAsModified(utcNow);
    }

    // Nothing went out: tried again after a growing wait (10 minutes, doubling, at most a day); after eight failed rounds this period is skipped.
    public void Failed(DateTimeOffset nextRegularRunUtc, DateTimeOffset utcNow)
    {
        Failures++;

        if (Failures >= MaxFailures)
        {
            (NextRunUtc, Failures) = (nextRegularRunUtc, 0);
        }
        else
        {
            NextRunUtc = utcNow + TimeSpan.FromMinutes(Math.Min(10 * Math.Pow(2, Failures - 1), 1440));
        }

        MarkAsModified(utcNow);
    }
}
