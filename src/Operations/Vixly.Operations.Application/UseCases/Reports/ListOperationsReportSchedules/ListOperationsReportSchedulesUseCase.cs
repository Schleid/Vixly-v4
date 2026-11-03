using Vixly.Operations.Application.Ports;
using Vixly.Operations.Application.Reports;
using Vixly.Operations.Application.Security;
using Vixly.Operations.Domain.Access;
using Vixly.Operations.Domain.Identity;
using Vixly.Operations.Domain.Reports;
using Vixly.Shared.Kernel.Domain;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Reports.ListOperationsReportSchedules;

// =========================================================
// FILE: ListOperationsReportSchedulesUseCase.cs
//
// PURPOSE:
// The scheduled exports this person set up (all of them for whoever edits settings), and the staff they can go to (Slice 11).
//
// RESPONSIBILITIES:
// - Run the rule and return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - Either report right; settings editors see every schedule
// =========================================================
public sealed class ListOperationsReportSchedulesUseCase
{
    private readonly OperationsAccess _access;

    private readonly IOperationsReportScheduleRepository _schedules;

    private readonly IOperationsUserRepository _users;

    public ListOperationsReportSchedulesUseCase(
        OperationsAccess access,
        IOperationsReportScheduleRepository schedules,
        IOperationsUserRepository users)
    {
        _access = access ?? throw new ArgumentNullException(nameof(access));
        _schedules = schedules ?? throw new ArgumentNullException(nameof(schedules));
        _users = users ?? throw new ArgumentNullException(nameof(users));
    }

    public async Task<ListOperationsReportSchedulesResponse> ExecuteAsync(
        OperationsAuthorizationContext context,
        ListOperationsReportSchedulesRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        if (await _access.LoadAsync(context, cancellationToken) is not { } access)
        {
            return ListOperationsReportSchedulesResponse.Failure(OperationsAccessErrors.SignInRequired);
        }

        if (!access.Has(OperationsPermissions.ReportsOperations) && !access.Has(OperationsPermissions.ReportsFinance))
        {
            return ListOperationsReportSchedulesResponse.Failure(OperationsAccessErrors.PermissionRequired);
        }

        bool all = access.HasTenantWide(OperationsPermissions.SettingsEdit);
        IReadOnlyList<OperationsReportSchedule> schedules = (await _schedules.ListAsync(cancellationToken)).Where(schedule => all || schedule.CreatedByUserId == access.User.Id).ToList();
        // Every active staff member (not partner agents), page by page (review: not only the first 200).
        List<OperationsUser> everyone = new();

        for (int page = 1; page <= 20; page++)
        {
            (IReadOnlyList<OperationsUser> items, int total) = await _users.ListAsync(null, page, 100, cancellationToken);
            everyone.AddRange(items);

            if (everyone.Count >= total || items.Count == 0)
            {
                break;
            }
        }

        IReadOnlyList<OperationsUser> staff = everyone.Where(user => user.Account.IsActive && user.PartnerWarehouseId is null)
            .OrderBy(user => user.FullName, StringComparer.CurrentCulture).ToList();
        return ListOperationsReportSchedulesResponse.Success(schedules, staff);
    }
}
