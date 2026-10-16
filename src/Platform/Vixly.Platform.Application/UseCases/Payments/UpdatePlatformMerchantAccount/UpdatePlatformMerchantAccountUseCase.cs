using Vixly.Platform.Application.Payments;
using Vixly.Platform.Application.Ports;
using Vixly.Platform.Application.Security;
using Vixly.Platform.Application.UseCases.Payments;
using Vixly.Platform.Domain.Billing;
using Vixly.Platform.Domain.Payments;
using Vixly.Platform.Domain.Tenants;
using Vixly.Shared.Kernel.Domain;

namespace Vixly.Platform.Application.UseCases.Payments.UpdatePlatformMerchantAccount;

// =========================================================
// FILE: UpdatePlatformMerchantAccountUseCase.cs
//
// PURPOSE:
// Changes the publishable key, accepted currencies and whether card payments are on (D-P8, D-P11).
//
// RESPONSIBILITIES:
// - Check the permission first (gate), then run the rule
// - Return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - One use case per behaviour (Global Standard 5.1)
// =========================================================
public sealed class UpdatePlatformMerchantAccountUseCase
{
    private readonly PlatformPaymentsAdministration _payments;

    public UpdatePlatformMerchantAccountUseCase(
        PlatformPaymentsAdministration payments)
    {
        _payments = payments ?? throw new ArgumentNullException(nameof(payments));
    }

    public async Task<UpdatePlatformMerchantAccountResponse> ExecuteAsync(
        PlatformAuthorizationContext context,
        UpdatePlatformMerchantAccountRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        if (await _payments.AuthorizeAsync(context, PlatformPermissions.PaymentsConfigure, requireStepUp: true, cancellationToken) is { } refused)
        {
            return UpdatePlatformMerchantAccountResponse.Failure(refused);
        }

        PlatformCardGatewayStatus status = _payments.Gateway.Status;

        // Keys come from configuration only; the mode is the configured key's, checked against the environment.
        if (request.Enabled && !status.Configured)
        {
            return UpdatePlatformMerchantAccountResponse.Failure(Vixly.Shared.Kernel.Errors.Error.Create(status.ProblemCode ?? PlatformPaymentsErrors.SecretMissing.Code));
        }

        PlatformMerchantAccount account = await _payments.MerchantAsync(cancellationToken);

        try
        {
            account.Update(status.Provider, status.Mode ?? PlatformPaymentMode.Test, request.PublishableKey, request.Currencies ?? Array.Empty<string>(), request.Enabled, context.PlatformUserId, _payments.UtcNow);
        }
        catch (DomainException exception)
        {
            return UpdatePlatformMerchantAccountResponse.Failure(exception.Error);
        }

        _payments.Audit(context, "platform.payments.merchant_updated", PlatformPermissions.PaymentsConfigure, nameof(PlatformMerchantAccount), account.Id,
            new { provider = account.Provider.ToString(), mode = account.Mode.ToString(), currencies = account.AcceptedCurrencies, enabled = account.CardPaymentsEnabled });

        return await _payments.SaveAsync(cancellationToken)
            ? UpdatePlatformMerchantAccountResponse.Success()
            : UpdatePlatformMerchantAccountResponse.Failure(PlatformCommonErrors.ConcurrencyConflict);
    }
}
