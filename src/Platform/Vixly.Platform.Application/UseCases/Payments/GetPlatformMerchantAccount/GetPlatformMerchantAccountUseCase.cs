using Vixly.Platform.Application.Payments;
using Vixly.Platform.Application.Ports;
using Vixly.Platform.Application.Security;
using Vixly.Platform.Application.UseCases.Payments;
using Vixly.Platform.Domain.Billing;
using Vixly.Platform.Domain.Payments;
using Vixly.Platform.Domain.Tenants;
using Vixly.Shared.Kernel.Domain;

namespace Vixly.Platform.Application.UseCases.Payments.GetPlatformMerchantAccount;

// =========================================================
// FILE: GetPlatformMerchantAccountUseCase.cs
//
// PURPOSE:
// Reads the merchant account and how the card provider is configured on this host (D-P8, D-P9).
//
// RESPONSIBILITIES:
// - Check the permission first (gate), then run the rule
// - Return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - One use case per behaviour (Global Standard 5.1)
// =========================================================
public sealed class GetPlatformMerchantAccountUseCase
{
    private readonly PlatformPaymentsAdministration _payments;

    public GetPlatformMerchantAccountUseCase(
        PlatformPaymentsAdministration payments)
    {
        _payments = payments ?? throw new ArgumentNullException(nameof(payments));
    }

    public async Task<GetPlatformMerchantAccountResponse> ExecuteAsync(
        PlatformAuthorizationContext context,
        GetPlatformMerchantAccountRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        if (await _payments.AuthorizeAsync(context, PlatformPermissions.BillingView, requireStepUp: false, cancellationToken) is { } refused)
        {
            return GetPlatformMerchantAccountResponse.Failure(refused);
        }

        PlatformMerchantAccount? account = await _payments.Store.GetMerchantAsync(cancellationToken);
        PlatformCardGatewayStatus status = _payments.Gateway.Status;

        return GetPlatformMerchantAccountResponse.Success(
            new PlatformMerchantAccountItem(
                account?.Provider ?? status.Provider,
                account?.Mode ?? status.Mode ?? PlatformPaymentMode.Test,
                account?.PublishableKey,
                account?.Currencies ?? Array.Empty<string>(),
                account?.CardPaymentsEnabled ?? false,
                account?.LastCheckedUtc,
                account?.LastCheckSucceeded ?? false,
                account?.LastCheckCode,
                status.Configured,
                status.Mode,
                status.ProblemCode,
                await _payments.ReadyAsync(cancellationToken)),
            await _payments.HoldsAsync(context, PlatformPermissions.PaymentsConfigure, cancellationToken));
    }
}
