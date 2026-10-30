using Vixly.Operations.Application.Billing;
using Vixly.Operations.Application.Ports;
using Vixly.Operations.Application.Security;
using Vixly.Operations.Domain.Access;
using Vixly.Operations.Domain.Billing;
using Vixly.Operations.Domain.Customers;
using Vixly.Operations.Domain.Settings;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Credit.GetOperationsCreditSettings;

// =========================================================
// FILE: GetOperationsCreditSettingsUseCase.cs
//
// PURPOSE:
// The company's credit settings (Slice 5F step 2b, K-2).
//
// RESPONSIBILITIES:
// - Run the rule and return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - ops.settings.view; saving needs ops.settings.edit
// =========================================================
public sealed class GetOperationsCreditSettingsUseCase
{
    private readonly OperationsAccess _access;

    private readonly IOperationsSettingsRepository _settings;

    private readonly TimeProvider _timeProvider;

    public GetOperationsCreditSettingsUseCase(
        OperationsAccess access,
        IOperationsSettingsRepository settings,
        TimeProvider timeProvider)
    {
        _access = access ?? throw new ArgumentNullException(nameof(access));
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public async Task<GetOperationsCreditSettingsResponse> ExecuteAsync(
        OperationsAuthorizationContext context,
        GetOperationsCreditSettingsRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);
        (OperationsAccessView? access, Error? refused) = await _access.RequireAsync(context, OperationsPermissions.SettingsView, false, cancellationToken);

        if (refused is not null)
        {
            return GetOperationsCreditSettingsResponse.Failure(refused);
        }

        OperationsTenantSettings settings = await _settings.FindAsync(cancellationToken) ?? OperationsTenantSettings.CreateDefault(context.RequiredTenantId, _timeProvider.GetUtcNow());
        return GetOperationsCreditSettingsResponse.Success(settings, access!.HasTenantWide(OperationsPermissions.SettingsEdit));
    }
}
