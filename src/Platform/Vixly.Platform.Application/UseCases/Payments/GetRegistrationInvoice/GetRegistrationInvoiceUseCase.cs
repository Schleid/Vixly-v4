using Vixly.Platform.Application.Payments;
using Vixly.Platform.Application.Ports;
using Vixly.Platform.Application.Security;
using Vixly.Platform.Application.UseCases.Payments;
using Vixly.Platform.Application.UseCases.Registration;
using Vixly.Platform.Domain.Billing;
using Vixly.Platform.Domain.Payments;
using Vixly.Platform.Domain.Tenants;
using Vixly.Shared.Kernel.Domain;

namespace Vixly.Platform.Application.UseCases.Payments.GetRegistrationInvoice;

// =========================================================
// FILE: GetRegistrationInvoiceUseCase.cs
//
// PURPOSE:
// Shows a self-registering company the invoice it must pay (D-P7).
//
// RESPONSIBILITIES:
// - Check the permission first (gate), then run the rule
// - Return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - Anonymous (public page, S-6); rate-limited by the API
// - Answers a code only; never says whether a token ever existed
// =========================================================
public sealed class GetRegistrationInvoiceUseCase
{
    private readonly PlatformPaymentsAdministration _payments;

    private readonly PlatformRegistrationLinks _links;

    private readonly PlatformZelleAdministration _zelle;

    public GetRegistrationInvoiceUseCase(
        PlatformPaymentsAdministration payments,
        PlatformRegistrationLinks links,
        PlatformZelleAdministration zelle)
    {
        _payments = payments ?? throw new ArgumentNullException(nameof(payments));
        _links = links ?? throw new ArgumentNullException(nameof(links));
        _zelle = zelle ?? throw new ArgumentNullException(nameof(zelle));
    }

    public async Task<GetRegistrationInvoiceResponse> ExecuteAsync(
        PlatformAnonymousAuthorizationContext context,
        GetRegistrationInvoiceRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        PlatformRegistrationAccess access = await _links.OpenAsync(request.AccessToken, _payments.UtcNow, cancellationToken);

        if (access.Registration is not { PlatformTenantId: Guid tenantId } registration)
        {
            return GetRegistrationInvoiceResponse.Failure(access.Error ?? PlatformRegistrationErrors.NotFound);
        }

        if (registration.Status != PlatformTenantRegistrationStatus.PendingPayment)
        {
            return GetRegistrationInvoiceResponse.Failure(PlatformPaymentsErrors.NothingToCharge);
        }

        PlatformTenant? tenant = await _payments.Tenants.GetAsync(tenantId, cancellationToken);
        PlatformInvoice? invoice = await PlatformRegistrationInvoice.FindAsync(_payments, tenantId, cancellationToken);

        if (tenant is null || invoice is null)
        {
            return GetRegistrationInvoiceResponse.Failure(PlatformPaymentsErrors.NothingToCharge);
        }

        return GetRegistrationInvoiceResponse.Success(new PlatformPaymentLinkSummary(
            PlatformPaymentLinkKind.PayInvoice, tenant.Company.DisplayName, invoice.Number, invoice.Balance, invoice.CurrencyCode, invoice.DueUtc, invoice.Balance <= 0m,
            await _payments.RefusalForAsync(invoice.CurrencyCode, cancellationToken) is null,
            PlatformPaymentsAdministration.ConsentTextVersion,
            await _zelle.OfferAsync(invoice, cancellationToken)));
    }
}
