using Vixly.Operations.Application.Ports;
using Vixly.Operations.Application.Reports;
using Vixly.Operations.Application.Security;
using Vixly.Operations.Domain.Access;
using Vixly.Operations.Domain.Locations;
using Vixly.Operations.Domain.Reports;
using Vixly.Operations.Domain.Settings;
using Vixly.Shared.Kernel.Domain;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Reports.ListOperationsReports;

// =========================================================
// FILE: ListOperationsReportsUseCase.cs
//
// PURPOSE:
// The Reports menu (Slice 11): the reports this person may run, by group.
//
// RESPONSIBILITIES:
// - Run the rule and return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - Whoever holds either report right; finance reports need the finance right; company-wide reports need it for the whole company
// =========================================================
public sealed class ListOperationsReportsUseCase
{
    private readonly OperationsAccess _access;

    public ListOperationsReportsUseCase(
        OperationsAccess access)
    {
        _access = access ?? throw new ArgumentNullException(nameof(access));
    }

    public async Task<ListOperationsReportsResponse> ExecuteAsync(
        OperationsAuthorizationContext context,
        ListOperationsReportsRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        // Either right, for any warehouse or the whole company.
        if (await _access.LoadAsync(context, cancellationToken) is not { } access)
        {
            return ListOperationsReportsResponse.Failure(OperationsAccessErrors.SignInRequired);
        }

        if (!access.Has(OperationsPermissions.ReportsOperations) && !access.Has(OperationsPermissions.ReportsFinance))
        {
            return ListOperationsReportsResponse.Failure(OperationsAccessErrors.PermissionRequired);
        }

        bool operations = access.Has(OperationsPermissions.ReportsOperations);
        bool finance = access.Has(OperationsPermissions.ReportsFinance);
        List<OperationsReportDefinition> reports = OperationsReportCatalog.All
            .Where(report => report.Finance ? finance : operations)
            .Where(report => !report.CompanyWide || access.HasTenantWide(report.Finance ? OperationsPermissions.ReportsFinance : OperationsPermissions.ReportsOperations))
            .ToList();
        return ListOperationsReportsResponse.Success(reports, finance, access.Has(OperationsPermissions.CostsView));
    }
}
