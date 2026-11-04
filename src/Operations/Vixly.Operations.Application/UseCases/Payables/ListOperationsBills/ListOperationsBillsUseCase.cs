using OperationsInvoice = Vixly.Operations.Domain.Billing.OperationsInvoice;
using Vixly.Operations.Application.Audit;
using Vixly.Operations.Application.Payables;
using Vixly.Operations.Application.Ports;
using Vixly.Operations.Application.Security;
using Vixly.Operations.Domain.Access;
using Vixly.Operations.Domain.Common;
using Vixly.Operations.Domain.Dispatch;
using Vixly.Operations.Domain.Locations;
using Vixly.Operations.Domain.Payables;
using Vixly.Operations.Domain.Settings;
using Vixly.Shared.Kernel.Domain;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Payables.ListOperationsBills;

// =========================================================
// FILE: ListOperationsBillsUseCase.cs
//
// PURPOSE:
// The company’s bills (Slice 8): what is owed, overdue and waiting for approval.
//
// RESPONSIBILITIES:
// - Run the rule and return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - Any of ops.bills.enter, ops.bills.approve or ops.costs.view (company-wide)
// =========================================================
public sealed class ListOperationsBillsUseCase
{
    private readonly OperationsPayablesDesk _desk;

    private readonly IOperationsSettingsRepository _settings;

    private readonly TimeProvider _timeProvider;

    public ListOperationsBillsUseCase(
        OperationsPayablesDesk desk,
        IOperationsSettingsRepository settings,
        TimeProvider timeProvider)
    {
        _desk = desk ?? throw new ArgumentNullException(nameof(desk));
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public async Task<ListOperationsBillsResponse> ExecuteAsync(
        OperationsAuthorizationContext context,
        ListOperationsBillsRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        (OperationsAccessView? access, Error? refused) = await _desk.OpenAsync(context, cancellationToken, OperationsPermissions.BillsApprove, OperationsPermissions.BillsEnter,
            OperationsPermissions.CostsView);

        if (refused is not null)
        {
            return ListOperationsBillsResponse.Failure(refused);
        }

        OperationsTenantSettings settings = await _settings.FindAsync(cancellationToken) ?? OperationsTenantSettings.CreateDefault(context.RequiredTenantId, _timeProvider.GetUtcNow());
        DateOnly today = OperationsTimeZones.Today(_timeProvider.GetUtcNow(), settings.TimeZone);
        (decimal owed, decimal overdue, int toApprove) = await _desk.Payables.OpenTotalsAsync(today, cancellationToken);
        IReadOnlyList<OperationsBill> bills = await _desk.Payables.ListBillsAsync(request.Status, 200, cancellationToken);

        return ListOperationsBillsResponse.Success(bills, owed, overdue, toApprove, today, access!.Has(OperationsPermissions.BillsEnter), access.Has(OperationsPermissions.BillsApprove),
            settings.BillsNeedApproval, access.Has(OperationsPermissions.SettingsEdit), settings.CurrencyCode);
    }
}
