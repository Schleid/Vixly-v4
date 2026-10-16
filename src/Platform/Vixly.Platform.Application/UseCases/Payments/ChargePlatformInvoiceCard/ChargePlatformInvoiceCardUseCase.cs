using Vixly.Platform.Application.Payments;
using Vixly.Platform.Application.Ports;
using Vixly.Platform.Application.Security;
using Vixly.Platform.Application.UseCases.Payments;
using Vixly.Platform.Domain.Billing;
using Vixly.Platform.Domain.Payments;
using Vixly.Platform.Domain.Tenants;
using Vixly.Shared.Kernel.Domain;

namespace Vixly.Platform.Application.UseCases.Payments.ChargePlatformInvoiceCard;

// =========================================================
// FILE: ChargePlatformInvoiceCardUseCase.cs
//
// PURPOSE:
// Charges the tenant's default saved card for an invoice's balance (D-P4).
//
// RESPONSIBILITIES:
// - Check the permission first (gate), then run the rule
// - Return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - One use case per behaviour (Global Standard 5.1)
// =========================================================
public sealed class ChargePlatformInvoiceCardUseCase
{
    private readonly PlatformPaymentsAdministration _payments;

    public ChargePlatformInvoiceCardUseCase(
        PlatformPaymentsAdministration payments)
    {
        _payments = payments ?? throw new ArgumentNullException(nameof(payments));
    }

    public async Task<ChargePlatformInvoiceCardResponse> ExecuteAsync(
        PlatformAuthorizationContext context,
        ChargePlatformInvoiceCardRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        if (await _payments.AuthorizeAsync(context, PlatformPermissions.BillingChargeCards, requireStepUp: true, cancellationToken) is { } refused)
        {
            return ChargePlatformInvoiceCardResponse.Failure(refused);
        }

        PlatformInvoice? invoice = await _payments.Billing.Invoices.GetAsync(request.PlatformInvoiceId, cancellationToken);

        if (invoice is null)
        {
            return ChargePlatformInvoiceCardResponse.Failure(PlatformBillingErrors.InvoiceNotFound);
        }

        if (invoice.Status != PlatformInvoiceStatus.Issued || invoice.Balance <= 0m)
        {
            return ChargePlatformInvoiceCardResponse.Failure(PlatformPaymentsErrors.NothingToCharge);
        }

        if (await _payments.RefusalForAsync(invoice.CurrencyCode, cancellationToken) is { } notReady)
        {
            return ChargePlatformInvoiceCardResponse.Failure(notReady);
        }

        PlatformSavedCard? card = await _payments.DefaultCardAsync(invoice.PlatformTenantId, cancellationToken);
        PlatformTenantCardProfile? profile = await _payments.Store.GetProfileAsync(invoice.PlatformTenantId, cancellationToken);

        if (card is null || profile is null)
        {
            return ChargePlatformInvoiceCardResponse.Failure(PlatformPaymentsErrors.NoSavedCard);
        }

        if (card.IsExpiredAt(_payments.UtcNow))
        {
            return ChargePlatformInvoiceCardResponse.Failure(PlatformPaymentsErrors.CardExpired);
        }

        if (await _payments.Store.GetOpenChargeAsync(invoice.Id, cancellationToken) is not null)
        {
            return ChargePlatformInvoiceCardResponse.Failure(PlatformPaymentsErrors.ChargeOpen);
        }

        // The amount is the balance, never typed (D-P4).
        PlatformCardTransaction charge = PlatformCardTransaction.Charge(invoice.PlatformTenantId, invoice.Id, PlatformCardTransactionOrigin.StaffCharge, card.Provider, invoice.Balance, invoice.CurrencyCode, card.Id, context.PlatformUserId, _payments.UtcNow);
        _payments.Store.AddTransaction(charge);
        _payments.Audit(context, "platform.payments.card_charge_started", PlatformPermissions.BillingChargeCards, nameof(PlatformInvoice), invoice.Id,
            new { number = invoice.Number, amount = PlatformPaymentsLimits.Format(charge.Amount), currency = charge.CurrencyCode, card = card.Brand + " " + card.Last4 });

        // Saved before the provider is asked: a second click or host finds the open charge.
        if (!await _payments.SaveAsync(cancellationToken))
        {
            return ChargePlatformInvoiceCardResponse.Failure(PlatformPaymentsErrors.ChargeOpen);
        }

        await _payments.ChargeSavedCardAsync(charge, profile, card, invoice, cancellationToken);
        await _payments.SaveAsync(cancellationToken);
        return ChargePlatformInvoiceCardResponse.Success(charge.Status, charge.FailureCode);
    }
}
