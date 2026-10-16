using Vixly.Platform.Application.Payments;
using Vixly.Platform.Application.Ports;
using Vixly.Platform.Application.Security;
using Vixly.Platform.Application.UseCases.Payments;
using Vixly.Platform.Domain.Billing;
using Vixly.Platform.Domain.Payments;
using Vixly.Platform.Domain.Tenants;
using Vixly.Shared.Kernel.Domain;

namespace Vixly.Platform.Application.UseCases.Payments.TurnOffPlatformAutoCharge;

// =========================================================
// FILE: TurnOffPlatformAutoChargeUseCase.cs
//
// PURPOSE:
// Stops automatic charging for a tenant; only the tenant can turn it back on (D-P4).
//
// RESPONSIBILITIES:
// - Check the permission first (gate), then run the rule
// - Return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - One use case per behaviour (Global Standard 5.1)
// =========================================================
public sealed class TurnOffPlatformAutoChargeUseCase
{
    private readonly PlatformPaymentsAdministration _payments;

    public TurnOffPlatformAutoChargeUseCase(
        PlatformPaymentsAdministration payments)
    {
        _payments = payments ?? throw new ArgumentNullException(nameof(payments));
    }

    public async Task<TurnOffPlatformAutoChargeResponse> ExecuteAsync(
        PlatformAuthorizationContext context,
        TurnOffPlatformAutoChargeRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        if (await _payments.AuthorizeAsync(context, PlatformPermissions.BillingChargeCards, requireStepUp: true, cancellationToken) is { } refused)
        {
            return TurnOffPlatformAutoChargeResponse.Failure(refused);
        }

        PlatformTenantCardProfile? profile = await _payments.Store.GetProfileAsync(request.PlatformTenantId, cancellationToken);

        if (profile is null)
        {
            return TurnOffPlatformAutoChargeResponse.Failure(PlatformPaymentsErrors.AutoChargeOff);
        }

        try
        {
            profile.TurnOffAutoCharge(context.PlatformUserId, _payments.UtcNow);
        }
        catch (DomainException exception)
        {
            return TurnOffPlatformAutoChargeResponse.Failure(exception.Error);
        }

        _payments.Audit(context, "platform.payments.auto_charge_off", PlatformPermissions.BillingChargeCards, nameof(PlatformTenant), request.PlatformTenantId);

        return await _payments.SaveAsync(cancellationToken)
            ? TurnOffPlatformAutoChargeResponse.Success()
            : TurnOffPlatformAutoChargeResponse.Failure(PlatformCommonErrors.ConcurrencyConflict);
    }
}
