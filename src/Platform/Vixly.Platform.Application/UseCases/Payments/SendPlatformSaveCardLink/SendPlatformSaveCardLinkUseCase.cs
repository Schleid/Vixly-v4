using Vixly.Platform.Application.Payments;
using Vixly.Platform.Application.Ports;
using Vixly.Platform.Application.Security;
using Vixly.Platform.Application.UseCases.Payments;
using Vixly.Platform.Domain.Billing;
using Vixly.Platform.Domain.Payments;
using Vixly.Platform.Domain.Tenants;
using Vixly.Shared.Kernel.Domain;

namespace Vixly.Platform.Application.UseCases.Payments.SendPlatformSaveCardLink;

// =========================================================
// FILE: SendPlatformSaveCardLinkUseCase.cs
//
// PURPOSE:
// Emails the tenant's billing contact a link to save a card on the provider's page (D-P3).
//
// RESPONSIBILITIES:
// - Check the permission first (gate), then run the rule
// - Return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - One use case per behaviour (Global Standard 5.1)
// =========================================================
public sealed class SendPlatformSaveCardLinkUseCase
{
    private readonly PlatformPaymentsAdministration _payments;

    private readonly IPlatformPaymentsMailer _mailer;

    public SendPlatformSaveCardLinkUseCase(
        PlatformPaymentsAdministration payments,
        IPlatformPaymentsMailer mailer)
    {
        _payments = payments ?? throw new ArgumentNullException(nameof(payments));
        _mailer = mailer ?? throw new ArgumentNullException(nameof(mailer));
    }

    public async Task<SendPlatformSaveCardLinkResponse> ExecuteAsync(
        PlatformAuthorizationContext context,
        SendPlatformSaveCardLinkRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        if (await _payments.AuthorizeAsync(context, PlatformPermissions.BillingChargeCards, requireStepUp: true, cancellationToken) is { } refused)
        {
            return SendPlatformSaveCardLinkResponse.Failure(refused);
        }

        if (!await _payments.ReadyAsync(cancellationToken))
        {
            return SendPlatformSaveCardLinkResponse.Failure(PlatformPaymentsErrors.NotReady);
        }

        PlatformTenant? tenant = await _payments.Tenants.GetAsync(request.PlatformTenantId, cancellationToken);

        if (tenant is null || tenant.Contacts.Count == 0)
        {
            return SendPlatformSaveCardLinkResponse.Failure(PlatformCommonErrors.NotFound);
        }

        (string token, PlatformPaymentLink link) = _payments.IssueLink(PlatformPaymentLinkKind.SaveCard, tenant.Id, null, context.PlatformUserId);
        _payments.Audit(context, "platform.payments.save_card_link_sent", PlatformPermissions.BillingChargeCards, nameof(PlatformTenant), tenant.Id, new { expires = link.ExpiresUtc });

        // Saved first, so the emailed link always works.
        if (!await _payments.SaveAsync(cancellationToken))
        {
            return SendPlatformSaveCardLinkResponse.Failure(PlatformCommonErrors.ConcurrencyConflict);
        }

        await _mailer.SendSaveCardLinkAsync(tenant, tenant.ContactFor(PlatformTenantContactRole.Billing), _payments.Links.SaveCardLink(token), link.ExpiresUtc, cancellationToken);
        return SendPlatformSaveCardLinkResponse.Success();
    }
}
