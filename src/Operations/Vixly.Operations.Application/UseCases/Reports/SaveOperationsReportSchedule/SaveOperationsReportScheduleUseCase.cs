using Vixly.Operations.Application.Ports;
using Vixly.Operations.Application.Reports;
using Vixly.Operations.Application.Security;
using Vixly.Operations.Domain.Access;
using Vixly.Operations.Domain.Identity;
using Vixly.Operations.Domain.Reports;
using Vixly.Shared.Kernel.Domain;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Reports.SaveOperationsReportSchedule;

// =========================================================
// FILE: SaveOperationsReportScheduleUseCase.cs
//
// PURPOSE:
// Sets a report to be emailed on a schedule to staff of the company (Slice 11; owner, 2026-10-22).
//
// RESPONSIBILITIES:
// - Run the rule and return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - The report’s right; recipients are active staff of the company (not partner agents); at most 50 schedules
// =========================================================
public sealed class SaveOperationsReportScheduleUseCase
{
    private readonly OperationsAccess _access;

    private readonly IOperationsReportScheduleRepository _schedules;

    private readonly IOperationsUserRepository _users;

    private readonly IOperationsSettingsRepository _settings;

    private readonly Vixly.Operations.Application.Audit.OperationsAuditTrail _audit;

    private readonly IOperationsUnitOfWork _unitOfWork;

    private readonly TimeProvider _timeProvider;

    public SaveOperationsReportScheduleUseCase(
        OperationsAccess access,
        IOperationsReportScheduleRepository schedules,
        IOperationsUserRepository users,
        IOperationsSettingsRepository settings,
        Vixly.Operations.Application.Audit.OperationsAuditTrail audit,
        IOperationsUnitOfWork unitOfWork,
        TimeProvider timeProvider)
    {
        _access = access ?? throw new ArgumentNullException(nameof(access));
        _schedules = schedules ?? throw new ArgumentNullException(nameof(schedules));
        _users = users ?? throw new ArgumentNullException(nameof(users));
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        _audit = audit ?? throw new ArgumentNullException(nameof(audit));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public async Task<SaveOperationsReportScheduleResponse> ExecuteAsync(
        OperationsAuthorizationContext context,
        SaveOperationsReportScheduleRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        if (await _access.LoadAsync(context, cancellationToken) is not { } access)
        {
            return SaveOperationsReportScheduleResponse.Failure(OperationsAccessErrors.SignInRequired);
        }

        // Whoever may run the report may schedule it; each recipient later gets it only as their own rights allow.
        if (OperationsReportCatalog.Find(request.Key) is not { } report)
        {
            return SaveOperationsReportScheduleResponse.Failure(OperationsReportErrors.NotFound);
        }

        if (!access.Has(report.Finance ? OperationsPermissions.ReportsFinance : OperationsPermissions.ReportsOperations))
        {
            return SaveOperationsReportScheduleResponse.Failure(OperationsAccessErrors.PermissionRequired);
        }

        IReadOnlyList<OperationsReportSchedule> existing = await _schedules.ListAsync(cancellationToken);

        if (existing.Count >= OperationsReportSchedule.MaxPerCompany)
        {
            return SaveOperationsReportScheduleResponse.Failure(OperationsReportErrors.TooManySchedules);
        }

        // Staff of the company only (owner, 2026-10-22).
        foreach (Guid recipient in request.Recipients.Distinct())
        {
            if (await _users.GetAsync(recipient, cancellationToken) is not { Account.IsActive: true, PartnerWarehouseId: null })
            {
                return SaveOperationsReportScheduleResponse.Failure(OperationsReportErrors.RecipientsInvalid);
            }
        }

        DateTimeOffset utcNow = _timeProvider.GetUtcNow();
        string zone = (await _settings.FindAsync(cancellationToken))?.TimeZone ?? "UTC";
        TimeZoneInfo timeZone = TimeZoneInfo.TryFindSystemTimeZoneById(zone, out TimeZoneInfo? found) ? found : TimeZoneInfo.Utc;
        OperationsReportSchedule schedule;

        try
        {
            schedule = OperationsReportSchedule.Create(context.RequiredTenantId, report.Key, request.Frequency, request.Format, request.WarehouseId, (int)request.Grain,
                request.Dimension is { } dimension ? (int)dimension : null, Math.Clamp(request.Days, 7, 730), request.Recipients, access.User.Id,
                OperationsReportScheduling.NextRun(request.Frequency, utcNow, timeZone), utcNow);
        }
        catch (DomainException exception)
        {
            return SaveOperationsReportScheduleResponse.Failure(exception.Error);
        }

        _schedules.Add(schedule);
        _audit.Record(access.User.Id, "ops.reports.schedule_added", nameof(OperationsReportSchedule), schedule.Id,
            after: new { schedule.ReportKey, frequency = schedule.Frequency.ToString(), format = schedule.Format.ToString(), recipients = schedule.Recipients.Count });

        if (!await _unitOfWork.SaveChangesAsync(cancellationToken))
        {
            _unitOfWork.DiscardChanges();
            return SaveOperationsReportScheduleResponse.Failure(Vixly.Operations.Domain.Common.OperationsCommonErrors.ConcurrencyConflict);
        }

        return SaveOperationsReportScheduleResponse.Success();
    }
}
