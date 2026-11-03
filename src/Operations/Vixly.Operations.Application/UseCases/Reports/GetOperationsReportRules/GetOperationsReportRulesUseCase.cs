using Vixly.Operations.Application.Ports;
using Vixly.Operations.Application.Reports;
using Vixly.Operations.Application.Security;
using Vixly.Operations.Domain.Access;
using Vixly.Operations.Domain.Locations;
using Vixly.Operations.Domain.Reports;
using Vixly.Operations.Domain.Settings;
using Vixly.Shared.Kernel.Domain;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Reports.GetOperationsReportRules;

// =========================================================
// FILE: GetOperationsReportRulesUseCase.cs
//
// PURPOSE:
// Settings → Reports (Slice 11): when a dispatch or a parcel counts as on time.
//
// RESPONSIBILITIES:
// - Run the rule and return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - Whoever may see settings
// =========================================================
public sealed class GetOperationsReportRulesUseCase
{
    private readonly OperationsAccess _access;

    private readonly IOperationsSettingsRepository _settings;

    private readonly IOperationsShippingServiceRepository _services;

    public GetOperationsReportRulesUseCase(
        OperationsAccess access,
        IOperationsSettingsRepository settings,
        IOperationsShippingServiceRepository services)
    {
        _access = access ?? throw new ArgumentNullException(nameof(access));
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        _services = services ?? throw new ArgumentNullException(nameof(services));
    }

    public async Task<GetOperationsReportRulesResponse> ExecuteAsync(
        OperationsAuthorizationContext context,
        GetOperationsReportRulesRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        (OperationsAccessView? access, Error? refused) = await _access.RequireAsync(context, OperationsPermissions.SettingsView, false, cancellationToken);

        if (refused is not null)
        {
            return GetOperationsReportRulesResponse.Failure(refused);
        }

        return GetOperationsReportRulesResponse.Success((await _settings.FindAsync(cancellationToken))?.DispatchGraceHours ?? 0,
            (await _services.ListAsync(cancellationToken)).Where(service => service.Offered).OrderBy(service => service.ServiceCode, StringComparer.Ordinal).ToList(),
            access!.Has(OperationsPermissions.SettingsEdit));
    }
}
