using Vixly.Platform.Application.Payments;
using Vixly.Platform.Application.Ports;
using Vixly.Platform.Application.Security;
using Vixly.Platform.Application.UseCases.Payments;
using Vixly.Platform.Domain.Billing;
using Vixly.Platform.Domain.Payments;
using Vixly.Platform.Domain.Tenants;
using Vixly.Shared.Kernel.Domain;

namespace Vixly.Platform.Application.UseCases.Payments.CheckPlatformMerchantAccount;

// =========================================================
// FILE: CheckPlatformMerchantAccountUseCase.cs
//
// PURPOSE:
// Asks the card provider whether the configured keys work, and records the answer (D-P9).
//
// RESPONSIBILITIES:
// - Check the permission first (gate), then run the rule
// - Return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - One use case per behaviour (Global Standard 5.1)
// =========================================================
public sealed class CheckPlatformMerchantAccountUseCase
{
    private readonly PlatformPaymentsAdministration _payments;

    public CheckPlatformMerchantAccountUseCase(
        PlatformPaymentsAdministration payments)
    {
        _payments = payments ?? throw new ArgumentNullException(nameof(payments));
    }

    public async Task<CheckPlatformMerchantAccountResponse> ExecuteAsync(
        PlatformAuthorizationContext context,
        CheckPlatformMerchantAccountRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        if (await _payments.AuthorizeAsync(context, PlatformPermissions.PaymentsConfigure, requireStepUp: false, cancellationToken) is { } refused)
        {
            return CheckPlatformMerchantAccountResponse.Failure(refused);
        }

        PlatformMerchantAccount account = await _payments.MerchantAsync(cancellationToken);
        PlatformCardGatewayStatus status = _payments.Gateway.Status;
        string? problem = status.Configured ? null : status.ProblemCode ?? PlatformPaymentsErrors.SecretMissing.Code;

        if (problem is null)
        {
            try
            {
                await _payments.Gateway.CheckAsync(cancellationToken);
            }
            catch (PlatformCardGatewayException exception)
            {
                problem = exception.Code;
            }
        }

        account.RecordCheck(problem is null, problem, _payments.UtcNow);
        _payments.Audit(context, "platform.payments.merchant_checked", PlatformPermissions.PaymentsConfigure, nameof(PlatformMerchantAccount), account.Id, new { connected = problem is null, code = problem });

        return await _payments.SaveAsync(cancellationToken)
            ? CheckPlatformMerchantAccountResponse.Success(problem is null, problem)
            : CheckPlatformMerchantAccountResponse.Failure(PlatformCommonErrors.ConcurrencyConflict);
    }
}
