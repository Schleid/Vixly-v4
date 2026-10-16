using Vixly.Platform.Application.Payments;
using Vixly.Platform.Application.Ports;
using Vixly.Platform.Application.Security;
using Vixly.Platform.Application.UseCases.Payments;
using Vixly.Platform.Domain.Billing;
using Vixly.Platform.Domain.Payments;
using Vixly.Platform.Domain.Tenants;
using Vixly.Shared.Kernel.Domain;

namespace Vixly.Platform.Application.UseCases.Payments.OpenPlatformPaymentLink;

// =========================================================
// FILE: OpenPlatformPaymentLinkUseCase.cs
//
// PURPOSE:
// Shows what a pay or save-card link is for, before the tenant continues to the provider.
//
// RESPONSIBILITIES:
// - Check the permission first (gate), then run the rule
// - Return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - Anonymous (public page, S-6); rate-limited by the API
// - Answers a code only; never says whether a token ever existed
// =========================================================
public sealed class OpenPlatformPaymentLinkUseCase
{
    private readonly PlatformPaymentsAdministration _payments;

    private readonly PlatformZelleAdministration _zelle;

    public OpenPlatformPaymentLinkUseCase(
        PlatformPaymentsAdministration payments,
        PlatformZelleAdministration zelle)
    {
        _payments = payments ?? throw new ArgumentNullException(nameof(payments));
        _zelle = zelle ?? throw new ArgumentNullException(nameof(zelle));
    }

    public async Task<OpenPlatformPaymentLinkResponse> ExecuteAsync(
        PlatformAnonymousAuthorizationContext context,
        OpenPlatformPaymentLinkRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        PlatformPaymentLink? link = await _payments.OpenLinkAsync(request.Token, cancellationToken);
        PlatformTenant? tenant = link is null ? null : await _payments.Tenants.GetAsync(link.PlatformTenantId, cancellationToken);

        if (link is null || tenant is null)
        {
            return OpenPlatformPaymentLinkResponse.Failure(PlatformPaymentsErrors.LinkInvalid);
        }

        if (link.Kind == PlatformPaymentLinkKind.SaveCard)
        {
            return OpenPlatformPaymentLinkResponse.Success(new PlatformPaymentLinkSummary(
                link.Kind, tenant.Company.DisplayName, null, null, null, null, false, await _payments.ReadyAsync(cancellationToken), PlatformPaymentsAdministration.ConsentTextVersion));
        }

        PlatformInvoice? invoice = await _payments.Billing.Invoices.GetAsync(link.PlatformInvoiceId!.Value, cancellationToken);

        if (invoice is null)
        {
            return OpenPlatformPaymentLinkResponse.Failure(PlatformPaymentsErrors.LinkInvalid);
        }

        bool settled = invoice.Status != PlatformInvoiceStatus.Issued || invoice.Balance <= 0m;

        return OpenPlatformPaymentLinkResponse.Success(new PlatformPaymentLinkSummary(
            link.Kind, tenant.Company.DisplayName, invoice.Number, invoice.Balance, invoice.CurrencyCode, invoice.DueUtc, settled,
            !settled && await _payments.RefusalForAsync(invoice.CurrencyCode, cancellationToken) is null,
            PlatformPaymentsAdministration.ConsentTextVersion,
            await _zelle.OfferAsync(invoice, cancellationToken)));
    }
}
