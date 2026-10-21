using Vixly.Operations.Application.Ports;
using Vixly.Operations.Application.Security;
using Vixly.Operations.Application.Settings;
using Vixly.Operations.Domain.Access;
using Vixly.Operations.Domain.Settings;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Settings.GetOperationsTenantSettings;

// =========================================================
// FILE: GetOperationsTenantSettingsUseCase.cs
//
// PURPOSE:
// Reads the tenant's settings, or the defaults before the first save.
//
// RESPONSIBILITIES:
// - Run the rule and return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - One use case per behaviour (Global Standard 5.1)
// =========================================================
public sealed class GetOperationsTenantSettingsUseCase
{
    private readonly OperationsAccess _access;

    private readonly IOperationsSettingsRepository _settings;

    private readonly TimeProvider _timeProvider;

    private readonly IOperationsBillingRepository _billing;

    public GetOperationsTenantSettingsUseCase(
        OperationsAccess access,
        IOperationsSettingsRepository settings,
        IOperationsBillingRepository billing,
        TimeProvider timeProvider)
    {
        _billing = billing ?? throw new ArgumentNullException(nameof(billing));
        _access = access ?? throw new ArgumentNullException(nameof(access));
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public async Task<GetOperationsTenantSettingsResponse> ExecuteAsync(
        OperationsAuthorizationContext context,
        GetOperationsTenantSettingsRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        (OperationsAccessView? access, Error? refused) = await _access.RequireAsync(context, OperationsPermissions.SettingsView, false, cancellationToken);

        if (refused is not null)
        {
            return GetOperationsTenantSettingsResponse.Failure(refused);
        }

        OperationsTenantSettings? saved = await _settings.FindAsync(cancellationToken);
        OperationsTenantSettings settings = saved ?? OperationsTenantSettings.CreateDefault(context.RequiredTenantId, _timeProvider.GetUtcNow());

        // Invoices arrive with Slice 9; until then the currency can always change.
        return GetOperationsTenantSettingsResponse.Success(new OperationsSettingsView(settings, saved is not null, await _billing.AnyInvoiceAsync(cancellationToken), access!.HasTenantWide(OperationsPermissions.SettingsEdit)));
    }
}
