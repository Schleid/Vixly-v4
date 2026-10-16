using Vixly.Platform.Application.Payments;
using Vixly.Platform.Application.Ports;
using Vixly.Platform.Application.Security;
using Vixly.Platform.Application.UseCases.Payments;
using Vixly.Platform.Application.UseCases.Registration;
using Vixly.Platform.Domain.Billing;
using Vixly.Platform.Domain.Payments;
using Vixly.Platform.Domain.Tenants;
using Vixly.Shared.Kernel.Domain;

namespace Vixly.Platform.Application.UseCases.Payments.StartRegistrationCheckout;

// =========================================================
// FILE: StartRegistrationCheckoutUseCase.cs
//
// PURPOSE:
// Opens the provider's hosted page for the registration invoice; the amount comes from the server (D-P7).
//
// RESPONSIBILITIES:
// - Check the permission first (gate), then run the rule
// - Return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - Anonymous (public page, S-6); rate-limited by the API
// - Answers a code only; never says whether a token ever existed
// =========================================================
public sealed class StartRegistrationCheckoutUseCase
{
    private readonly PlatformPaymentsAdministration _payments;

    private readonly PlatformRegistrationLinks _links;

    public StartRegistrationCheckoutUseCase(
        PlatformPaymentsAdministration payments,
        PlatformRegistrationLinks links)
    {
        _payments = payments ?? throw new ArgumentNullException(nameof(payments));
        _links = links ?? throw new ArgumentNullException(nameof(links));
    }

    public async Task<StartRegistrationCheckoutResponse> ExecuteAsync(
        PlatformAnonymousAuthorizationContext context,
        StartRegistrationCheckoutRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        PlatformRegistrationAccess access = await _links.OpenAsync(request.AccessToken, _payments.UtcNow, cancellationToken);

        if (access.Registration is not { PlatformTenantId: Guid tenantId } registration)
        {
            return StartRegistrationCheckoutResponse.Failure(access.Error ?? PlatformRegistrationErrors.NotFound);
        }

        PlatformTenant? tenant = await _payments.Tenants.GetAsync(tenantId, cancellationToken);
        PlatformInvoice? invoice = await PlatformRegistrationInvoice.FindAsync(_payments, tenantId, cancellationToken);

        if (registration.Status != PlatformTenantRegistrationStatus.PendingPayment || tenant is null || invoice is null || invoice.Balance <= 0m)
        {
            return StartRegistrationCheckoutResponse.Failure(PlatformPaymentsErrors.NothingToCharge);
        }

        if (await _payments.RefusalForAsync(invoice.CurrencyCode, cancellationToken) is { } notReady)
        {
            return StartRegistrationCheckoutResponse.Failure(notReady);
        }

        string page = _payments.Links.RegistrationPayment();

        try
        {
            PlatformCheckoutSession session = await _payments.OpenPaymentCheckoutAsync(tenant, invoice, PlatformCardTransactionOrigin.Registration, request.SaveCard, request.AutoCharge, page + "?status=done", page + "?status=cancelled", cancellationToken);
            return StartRegistrationCheckoutResponse.Success(session.Url);
        }
        catch (DomainException exception)
        {
            return StartRegistrationCheckoutResponse.Failure(exception.Error);
        }
        catch (PlatformCardGatewayException)
        {
            _payments.UnitOfWork.DiscardChanges();
            return StartRegistrationCheckoutResponse.Failure(PlatformPaymentsErrors.ProviderUnavailable);
        }
    }
}
