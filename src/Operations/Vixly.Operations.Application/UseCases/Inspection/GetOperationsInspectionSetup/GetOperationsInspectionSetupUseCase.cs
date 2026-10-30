using Vixly.Operations.Application.Audit;
using Vixly.Operations.Application.Ports;
using Vixly.Operations.Application.Security;
using Vixly.Operations.Domain.Access;
using Vixly.Operations.Domain.Common;
using Vixly.Operations.Domain.Inspection;
using Vixly.Operations.Domain.Settings;
using Vixly.Shared.Kernel.Domain;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Inspection.GetOperationsInspectionSetup;

// =========================================================
// FILE: GetOperationsInspectionSetupUseCase.cs
//
// PURPOSE:
// The company's restricted-items list, inspection rules and random share (Slice 3C design section 3).
//
// RESPONSIBILITIES:
// - Run the rule and return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - Company-wide: only a grant for the whole company opens it (ops.inspection.configure)
// =========================================================
public sealed class GetOperationsInspectionSetupUseCase
{
    private readonly OperationsAccess _access;

    private readonly IOperationsInspectionRepository _inspections;

    private readonly IOperationsSettingsRepository _settings;

    public GetOperationsInspectionSetupUseCase(
        OperationsAccess access,
        IOperationsInspectionRepository inspections,
        IOperationsSettingsRepository settings)
    {
        _access = access ?? throw new ArgumentNullException(nameof(access));
        _inspections = inspections ?? throw new ArgumentNullException(nameof(inspections));
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
    }

    public async Task<GetOperationsInspectionSetupResponse> ExecuteAsync(
        OperationsAuthorizationContext context,
        GetOperationsInspectionSetupRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        (OperationsAccessView? _, Error? refused) = await _access.RequireAsync(context, OperationsPermissions.InspectionConfigure, false, cancellationToken);

        if (refused is not null)
        {
            return GetOperationsInspectionSetupResponse.Failure(refused);
        }

        OperationsTenantSettings? settings = await _settings.FindAsync(cancellationToken);
        return GetOperationsInspectionSetupResponse.Success(await _inspections.ListItemsAsync(cancellationToken), await _inspections.ListRulesAsync(cancellationToken),
            settings?.InspectionPercent ?? OperationsTenantSettings.DefaultInspectionPercent, settings?.CurrencyCode ?? OperationsTenantSettings.DefaultCurrency);
    }
}
