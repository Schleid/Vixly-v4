using Vixly.Operations.Application.Ports;
using Vixly.Operations.Application.Reports;
using Vixly.Operations.Application.Security;
using Vixly.Operations.Domain.Access;
using Vixly.Operations.Domain.Identity;
using Vixly.Operations.Domain.Reports;
using Vixly.Shared.Kernel.Domain;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Reports.RemoveOperationsReportSchedule;

// =========================================================
// FILE: RemoveOperationsReportScheduleUseCase.cs
//
// PURPOSE:
// Stops a scheduled export (Slice 11).
//
// RESPONSIBILITIES:
// - Run the rule and return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - Its maker or a settings editor
// =========================================================
public sealed class RemoveOperationsReportScheduleUseCase
{
    private readonly OperationsAccess _access;

    private readonly IOperationsReportScheduleRepository _schedules;

    private readonly Vixly.Operations.Application.Audit.OperationsAuditTrail _audit;

    private readonly IOperationsUnitOfWork _unitOfWork;

    public RemoveOperationsReportScheduleUseCase(
        OperationsAccess access,
        IOperationsReportScheduleRepository schedules,
        Vixly.Operations.Application.Audit.OperationsAuditTrail audit,
        IOperationsUnitOfWork unitOfWork)
    {
        _access = access ?? throw new ArgumentNullException(nameof(access));
        _schedules = schedules ?? throw new ArgumentNullException(nameof(schedules));
        _audit = audit ?? throw new ArgumentNullException(nameof(audit));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
    }

    public async Task<RemoveOperationsReportScheduleResponse> ExecuteAsync(
        OperationsAuthorizationContext context,
        RemoveOperationsReportScheduleRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        if (await _access.LoadAsync(context, cancellationToken) is not { } access)
        {
            return RemoveOperationsReportScheduleResponse.Failure(OperationsAccessErrors.SignInRequired);
        }

        // Its maker, or whoever edits settings.
        if (await _schedules.GetAsync(request.ScheduleId, cancellationToken) is not { } schedule
            || (schedule.CreatedByUserId != access.User.Id && !access.HasTenantWide(OperationsPermissions.SettingsEdit)))
        {
            return RemoveOperationsReportScheduleResponse.Failure(OperationsReportErrors.ScheduleNotFound);
        }

        _schedules.Remove(schedule);
        _audit.Record(access.User.Id, "ops.reports.schedule_removed", nameof(OperationsReportSchedule), schedule.Id, before: new { schedule.ReportKey });

        if (!await _unitOfWork.SaveChangesAsync(cancellationToken))
        {
            _unitOfWork.DiscardChanges();
            return RemoveOperationsReportScheduleResponse.Failure(Vixly.Operations.Domain.Common.OperationsCommonErrors.ConcurrencyConflict);
        }

        return RemoveOperationsReportScheduleResponse.Success();
    }
}
