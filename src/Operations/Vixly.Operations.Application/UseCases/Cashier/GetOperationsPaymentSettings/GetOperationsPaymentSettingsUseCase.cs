using Vixly.Operations.Application.Ports;
using Vixly.Operations.Application.Security;
using Vixly.Operations.Domain.Access;
using Vixly.Operations.Domain.Settings;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Cashier.GetOperationsPaymentSettings;

// =========================================================
// FILE: GetOperationsPaymentSettingsUseCase.cs
//
// PURPOSE:
// Settings → Payments (Slice 5F, C-1): the methods the company accepts and its Zelle details.
//
// RESPONSIBILITIES:
// - Run the rule and return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - ops.settings.view to read; ops.settings.edit to change
// =========================================================
public sealed class GetOperationsPaymentSettingsUseCase
{
    private readonly OperationsAccess _access;

    private readonly IOperationsSettingsRepository _settings;

    private readonly TimeProvider _timeProvider;

    public GetOperationsPaymentSettingsUseCase(
        OperationsAccess access,
        IOperationsSettingsRepository settings,
        TimeProvider timeProvider)
    {
        _access = access ?? throw new ArgumentNullException(nameof(access));
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public async Task<GetOperationsPaymentSettingsResponse> ExecuteAsync(
        OperationsAuthorizationContext context,
        GetOperationsPaymentSettingsRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        (OperationsAccessView? access, Error? refused) = await _access.RequireAsync(context, OperationsPermissions.SettingsView, false, cancellationToken);

        if (refused is not null)
        {
            return GetOperationsPaymentSettingsResponse.Failure(refused);
        }

        return GetOperationsPaymentSettingsResponse.Success(
            await _settings.FindAsync(cancellationToken) ?? OperationsTenantSettings.CreateDefault(context.RequiredTenantId, _timeProvider.GetUtcNow()),
            access!.HasTenantWide(OperationsPermissions.SettingsEdit));
    }
}
