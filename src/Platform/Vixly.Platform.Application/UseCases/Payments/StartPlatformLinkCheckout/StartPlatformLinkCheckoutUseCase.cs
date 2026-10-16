using Vixly.Platform.Application.Payments;
using Vixly.Platform.Application.Ports;
using Vixly.Platform.Application.Security;
using Vixly.Platform.Application.UseCases.Payments;
using Vixly.Platform.Domain.Billing;
using Vixly.Platform.Domain.Payments;
using Vixly.Platform.Domain.Tenants;
using Vixly.Shared.Kernel.Domain;

namespace Vixly.Platform.Application.UseCases.Payments.StartPlatformLinkCheckout;

// =========================================================
// FILE: StartPlatformLinkCheckoutUseCase.cs
//
// PURPOSE:
// Opens the provider's hosted page from a pay or save-card link (D-P2, D-P7).
//
// RESPONSIBILITIES:
// - Check the permission first (gate), then run the rule
// - Return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - Anonymous (public page, S-6); rate-limited by the API
// - Answers a code only; never says whether a token ever existed
// =========================================================
public sealed class StartPlatformLinkCheckoutUseCase
{
    private readonly PlatformPaymentsAdministration _payments;

    public StartPlatformLinkCheckoutUseCase(
        PlatformPaymentsAdministration payments)
    {
        _payments = payments ?? throw new ArgumentNullException(nameof(payments));
    }

    public async Task<StartPlatformLinkCheckoutResponse> ExecuteAsync(
        PlatformAnonymousAuthorizationContext context,
        StartPlatformLinkCheckoutRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        PlatformPaymentLink? link = await _payments.OpenLinkAsync(request.Token, cancellationToken);
        PlatformTenant? tenant = link is null ? null : await _payments.Tenants.GetAsync(link.PlatformTenantId, cancellationToken);

        if (link is null || tenant is null || tenant.Contacts.Count == 0)
        {
            return StartPlatformLinkCheckoutResponse.Failure(PlatformPaymentsErrors.LinkInvalid);
        }

        string page = _payments.Links.PayLink(request.Token!.Trim());

        try
        {
            if (link.Kind == PlatformPaymentLinkKind.SaveCard)
            {
                if (!await _payments.ReadyAsync(cancellationToken))
                {
                    return StartPlatformLinkCheckoutResponse.Failure(PlatformPaymentsErrors.NotReady);
                }

                page = _payments.Links.SaveCardLink(request.Token.Trim());
                PlatformCheckoutSession setup = await _payments.OpenSaveCardCheckoutAsync(tenant, link, request.AutoCharge, page + "?status=done", page + "?status=cancelled", cancellationToken);
                return StartPlatformLinkCheckoutResponse.Success(setup.Url);
            }

            PlatformInvoice? invoice = await _payments.Billing.Invoices.GetAsync(link.PlatformInvoiceId!.Value, cancellationToken);

            if (invoice is null || invoice.Status != PlatformInvoiceStatus.Issued || invoice.Balance <= 0m)
            {
                return StartPlatformLinkCheckoutResponse.Failure(PlatformPaymentsErrors.NothingToCharge);
            }

            if (await _payments.RefusalForAsync(invoice.CurrencyCode, cancellationToken) is { } notReady)
            {
                return StartPlatformLinkCheckoutResponse.Failure(notReady);
            }

            PlatformCheckoutSession session = await _payments.OpenPaymentCheckoutAsync(tenant, invoice, PlatformCardTransactionOrigin.PayLink, request.SaveCard, request.AutoCharge, page + "?status=done", page + "?status=cancelled", cancellationToken);
            return StartPlatformLinkCheckoutResponse.Success(session.Url);
        }
        catch (DomainException exception)
        {
            return StartPlatformLinkCheckoutResponse.Failure(exception.Error);
        }
        catch (PlatformCardGatewayException)
        {
            _payments.UnitOfWork.DiscardChanges();
            return StartPlatformLinkCheckoutResponse.Failure(PlatformPaymentsErrors.ProviderUnavailable);
        }
    }
}
