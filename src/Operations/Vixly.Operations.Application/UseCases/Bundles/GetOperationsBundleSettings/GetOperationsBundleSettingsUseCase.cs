using Vixly.Operations.Application.Ports;
using Vixly.Operations.Application.Security;
using Vixly.Operations.Domain.Access;
using Vixly.Operations.Domain.Bundles;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Bundles.GetOperationsBundleSettings;

// =========================================================
// FILE: GetOperationsBundleSettingsUseCase.cs
//
// PURPOSE:
// The company's bundle settings (Slice 6A, D-1): its bundle types, the target rule and the consolidation way.
//
// RESPONSIBILITIES:
// - Run the rule and return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - ops.settings.view; editing needs ops.settings.edit
// =========================================================
public sealed class GetOperationsBundleSettingsUseCase
{
    private readonly OperationsAccess _access;

    private readonly IOperationsSettingsRepository _settings;

    private readonly TimeProvider _timeProvider;

    public GetOperationsBundleSettingsUseCase(
        OperationsAccess access,
        IOperationsSettingsRepository settings,
        TimeProvider timeProvider)
    {
        _access = access ?? throw new ArgumentNullException(nameof(access));
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public async Task<GetOperationsBundleSettingsResponse> ExecuteAsync(
        OperationsAuthorizationContext context,
        GetOperationsBundleSettingsRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);
        (OperationsAccessView? access, Error? refused) = await _access.RequireAsync(context, OperationsPermissions.SettingsView, false, cancellationToken);

        if (refused is not null)
        {
            return GetOperationsBundleSettingsResponse.Failure(refused);
        }

        Vixly.Operations.Domain.Settings.OperationsTenantSettings settings = await _settings.FindAsync(cancellationToken)
            ?? Vixly.Operations.Domain.Settings.OperationsTenantSettings.CreateDefault(context.RequiredTenantId, _timeProvider.GetUtcNow());
        return GetOperationsBundleSettingsResponse.Success(settings.BundleTypeList(), settings.BundleTargetRule, settings.ConsolidationWay, access!.HasTenantWide(OperationsPermissions.SettingsEdit));
    }
}
