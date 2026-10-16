using Vixly.Platform.Application.Security;
using Vixly.Platform.Application.UseCases.Payments;
using Vixly.Platform.Domain.Billing;
using Vixly.Platform.Domain.Payments;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Platform.Application.UseCases.TenantAccount.CreateTenantPayLink;

// =========================================================
// FILE: CreateTenantPayLinkUseCase.cs
//
// PURPOSE:
// A payment link for one of the tenant's open invoices (call 23).
//
// RESPONSIBILITIES:
// - Check the permission first (gate), then run the rule
// - Return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - Called by Operations over the internal channel for the tenant in the service token only (S-13, D-K8)
// - Owner, admin or billing role
// - The token is returned once, inside the link; only its hash is kept
// =========================================================
public sealed class CreateTenantPayLinkUseCase
{
    private readonly PlatformPaymentsAdministration _payments;

    private readonly PlatformZelleAdministration _zelle;

    public CreateTenantPayLinkUseCase(
        PlatformPaymentsAdministration payments,
        PlatformZelleAdministration zelle)
    {
        _payments = payments ?? throw new ArgumentNullException(nameof(payments));
        _zelle = zelle ?? throw new ArgumentNullException(nameof(zelle));
    }

    public async Task<CreateTenantPayLinkResponse> ExecuteAsync(
        PlatformTenantAuthorizationContext context,
        CreateTenantPayLinkRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        Error? notAllowed = PlatformTenantAccess.Require(context, false, PlatformTenantActorRole.Owner, PlatformTenantActorRole.Admin, PlatformTenantActorRole.Billing);

        if (notAllowed is not null)
        {
            return CreateTenantPayLinkResponse.Failure(notAllowed);
        }

        PlatformInvoice? invoice = await _payments.Billing.Invoices.GetAsync(request.PlatformInvoiceId, cancellationToken);

        if (invoice is null || invoice.PlatformTenantId != context.PlatformTenantId)
        {
            return CreateTenantPayLinkResponse.Failure(PlatformBillingErrors.InvoiceNotFound);
        }

        if (invoice.Status != PlatformInvoiceStatus.Issued || invoice.Balance <= 0m)
        {
            return CreateTenantPayLinkResponse.Failure(invoice.Status == PlatformInvoiceStatus.Void ? PlatformBillingErrors.InvoiceVoid : PlatformBillingErrors.InvoicePaid);
        }

        // Cards or Zelle must be on offer (Slice 5 and the Zelle update).
        if (await _payments.RefusalForAsync(invoice.CurrencyCode, cancellationToken) is { } refused && await _zelle.OfferAsync(invoice, cancellationToken) is null)
        {
            return CreateTenantPayLinkResponse.Failure(refused);
        }

        (string token, PlatformPaymentLink link) = _payments.IssueLink(PlatformPaymentLinkKind.PayInvoice, invoice.PlatformTenantId, invoice.Id, null);

        return await _payments.SaveAsync(cancellationToken)
            ? CreateTenantPayLinkResponse.Success(_payments.Links.PayLink(token), link.ExpiresUtc)
            : CreateTenantPayLinkResponse.Failure(PlatformCommonErrors.ConcurrencyConflict);
    }
}
