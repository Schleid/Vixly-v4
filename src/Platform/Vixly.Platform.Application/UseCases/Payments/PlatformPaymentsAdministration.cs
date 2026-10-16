using Vixly.Platform.Application.Audit;
using Vixly.Platform.Application.Messaging;
using Vixly.Platform.Application.Payments;
using Vixly.Platform.Application.Ports;
using Vixly.Platform.Application.Security;
using Vixly.Platform.Application.UseCases.Billing;
using Vixly.Platform.Domain.Billing;
using Vixly.Platform.Domain.Payments;
using Vixly.Platform.Domain.Tenants;
using Vixly.Shared.Kernel.Domain;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Platform.Application.UseCases.Payments;

// =========================================================
// FILE: PlatformPaymentsAdministration.cs
//
// PURPOSE:
// The shared steps of Slice 5 use cases, handlers and the clock.
//
// RESPONSIBILITIES:
// - Authorize through Billing; know whether card payments are ready for a currency
// - Find or create the tenant's provider customer; issue and open payment links
// - Open hosted checkouts and charge saved cards with one open charge per invoice
// - Record failures for staff, the tenant and Slice 6
//
// IMPORTANT NOTES:
// - Saves before calling the provider, so the unique open-charge index stops a second charge (D-P5)
// - A provider timeout leaves the transaction open for the clock; it is never marked failed
// =========================================================
public sealed class PlatformPaymentsAdministration
{
    // The automatic-charging consent text the public page shows; bump it when the text changes.
    public const string ConsentTextVersion = "2026-10";

    private readonly PlatformBillingAdministration _billing;

    private readonly IPlatformPaymentsRepository _payments;

    private readonly IPlatformCardGateway _gateway;

    private readonly IPlatformTenantRepository _tenants;

    private readonly IPlatformSecretGenerator _secrets;

    private readonly IPlatformPublicLinks _links;

    private readonly PlatformAuditTrail _audit;

    private readonly IPlatformUnitOfWork _unitOfWork;

    public PlatformPaymentsAdministration(
        PlatformBillingAdministration billing,
        IPlatformPaymentsRepository payments,
        IPlatformCardGateway gateway,
        IPlatformTenantRepository tenants,
        IPlatformSecretGenerator secrets,
        IPlatformPublicLinks links,
        PlatformAuditTrail audit,
        IPlatformUnitOfWork unitOfWork)
    {
        _billing = billing ?? throw new ArgumentNullException(nameof(billing));
        _payments = payments ?? throw new ArgumentNullException(nameof(payments));
        _gateway = gateway ?? throw new ArgumentNullException(nameof(gateway));
        _tenants = tenants ?? throw new ArgumentNullException(nameof(tenants));
        _secrets = secrets ?? throw new ArgumentNullException(nameof(secrets));
        _links = links ?? throw new ArgumentNullException(nameof(links));
        _audit = audit ?? throw new ArgumentNullException(nameof(audit));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
    }

    public DateTimeOffset UtcNow => _billing.UtcNow;

    public PlatformBillingAdministration Billing => _billing;

    public IPlatformPaymentsRepository Store => _payments;

    public IPlatformCardGateway Gateway => _gateway;

    public IPlatformTenantRepository Tenants => _tenants;

    public IPlatformPublicLinks Links => _links;

    public IPlatformUnitOfWork UnitOfWork => _unitOfWork;

    public Task<Error?> AuthorizeAsync(PlatformAuthorizationContext context, string permissionCode, bool requireStepUp, CancellationToken cancellationToken)
    {
        return _billing.AuthorizeAsync(context, permissionCode, requireStepUp, cancellationToken);
    }

    public Task<bool> HoldsAsync(PlatformAuthorizationContext context, string permissionCode, CancellationToken cancellationToken)
    {
        return _billing.HoldsAsync(context, permissionCode, cancellationToken);
    }

    public void Audit(PlatformAuthorizationContext? context, string actionCode, string? permissionCode, string targetType, Guid? targetId, object? after = null)
    {
        _audit.Record(context?.PlatformUserId, actionCode, permissionCode, targetType, targetId, after: after);
    }

    public async Task<bool> SaveAsync(CancellationToken cancellationToken)
    {
        if (await _unitOfWork.SaveChangesAsync(cancellationToken))
        {
            return true;
        }

        _unitOfWork.DiscardChanges();
        return false;
    }

    // =====================================================
    // METHOD: READINESS (D-P9, D-P11)
    // =====================================================
    public async Task<PlatformMerchantAccount> MerchantAsync(CancellationToken cancellationToken)
    {
        PlatformMerchantAccount? account = await _payments.GetMerchantAsync(cancellationToken);

        if (account is null)
        {
            account = PlatformMerchantAccount.Create(_gateway.Status.Provider, UtcNow);
            _payments.AddMerchant(account);
        }

        return account;
    }

    // Null when card payments can be taken in this currency on this host.
    public async Task<Error?> RefusalForAsync(string? currencyCode, CancellationToken cancellationToken)
    {
        PlatformCardGatewayStatus status = _gateway.Status;
        PlatformMerchantAccount? account = await _payments.GetMerchantAsync(cancellationToken);

        if (!status.Configured || account is null || !account.IsReady || account.Provider != status.Provider || account.Mode != status.Mode)
        {
            return PlatformPaymentsErrors.NotReady;
        }

        return currencyCode is not null && account.Currencies.Contains(currencyCode, StringComparer.Ordinal)
            ? null
            : PlatformPaymentsErrors.CurrencyNotAccepted;
    }

    public async Task<bool> ReadyAsync(CancellationToken cancellationToken)
    {
        PlatformCardGatewayStatus status = _gateway.Status;
        PlatformMerchantAccount? account = await _payments.GetMerchantAsync(cancellationToken);

        return status.Configured && account is not null && account.IsReady && account.Provider == status.Provider && account.Mode == status.Mode;
    }

    // =====================================================
    // METHOD: CUSTOMER AND CARDS (D-P3)
    // =====================================================
    public async Task<PlatformTenantCardProfile> ProfileAsync(PlatformTenant tenant, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(tenant);

        PlatformCardGatewayStatus status = _gateway.Status;
        PlatformPaymentMode mode = status.Mode ?? throw new DomainException(PlatformPaymentsErrors.NotReady);
        PlatformTenantCardProfile? profile = await _payments.GetProfileAsync(tenant.Id, cancellationToken);

        if (profile is not null && profile.Matches(status.Provider, mode))
        {
            return profile;
        }

        PlatformTenantContact contact = tenant.ContactFor(PlatformTenantContactRole.Billing);
        string customerId = await _gateway.CreateCustomerAsync(
            tenant.Company.LegalName,
            contact.Email.Value,
            tenant.Id,
            "vx4-cus-" + tenant.Id.ToString("N", System.Globalization.CultureInfo.InvariantCulture) + "-" + mode.ToString().ToLowerInvariant(),
            cancellationToken);

        if (profile is null)
        {
            profile = PlatformTenantCardProfile.Create(tenant.Id, status.Provider, mode, customerId, UtcNow);
            _payments.AddProfile(profile);
        }
        else
        {
            profile.Reconnect(status.Provider, mode, customerId, UtcNow);

            foreach (PlatformSavedCard card in (await _payments.ListCardsAsync(tenant.Id, cancellationToken)).Where(card => card.IsActive))
            {
                card.Remove(null, UtcNow);
            }
        }

        return profile;
    }

    public async Task<PlatformSavedCard?> DefaultCardAsync(Guid tenantId, CancellationToken cancellationToken)
    {
        PlatformCardProvider provider = _gateway.Status.Provider;

        return (await _payments.ListCardsAsync(tenantId, cancellationToken))
            .FirstOrDefault(card => card.IsActive && card.IsDefault && card.Provider == provider);
    }

    public static PlatformSavedCardItem CardItem(PlatformSavedCard card, DateTimeOffset utcNow)
    {
        ArgumentNullException.ThrowIfNull(card);

        return new PlatformSavedCardItem(card.Id, card.Brand, card.Last4, card.ExpiryMonth, card.ExpiryYear, card.IsDefault, card.IsExpiredAt(utcNow), card.AddedVia, card.CreatedUtc);
    }

    // =====================================================
    // METHOD: LINKS
    // =====================================================
    public (string Token, PlatformPaymentLink Link) IssueLink(PlatformPaymentLinkKind kind, Guid tenantId, Guid? invoiceId, Guid? actorUserId)
    {
        PlatformSecret secret = _secrets.NewToken();
        PlatformPaymentLink link = PlatformPaymentLink.Create(kind, tenantId, invoiceId, secret.Hash, actorUserId, UtcNow);
        _payments.AddLink(link);
        return (secret.Value, link);
    }

    public async Task<PlatformPaymentLink?> OpenLinkAsync(string? token, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(token) || token.Length > 200)
        {
            return null;
        }

        PlatformPaymentLink? link = await _payments.GetLinkAsync(_secrets.HashToken(token.Trim()), cancellationToken);
        return link is not null && link.IsUsableAt(UtcNow) ? link : null;
    }

    // =====================================================
    // METHOD: CHECKOUT (D-P2, D-P7)
    // =====================================================
    public async Task<PlatformCheckoutSession> OpenPaymentCheckoutAsync(
        PlatformTenant tenant,
        PlatformInvoice invoice,
        PlatformCardTransactionOrigin origin,
        bool saveCard,
        bool autoCharge,
        string successUrl,
        string cancelUrl,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(tenant);
        ArgumentNullException.ThrowIfNull(invoice);

        PlatformCardTransaction? open = await _payments.GetOpenChargeAsync(invoice.Id, cancellationToken);

        if (open is not null)
        {
            // An abandoned checkout gives way to a new one; anything already with the bank does not.
            if (open.Status != PlatformCardTransactionStatus.Created || open.Origin is not (PlatformCardTransactionOrigin.PayLink or PlatformCardTransactionOrigin.Registration))
            {
                throw new DomainException(PlatformPaymentsErrors.ChargeOpen);
            }

            if (open.ProviderCheckoutId is { } previous)
            {
                await _gateway.ExpireCheckoutAsync(previous, cancellationToken);
            }

            open.Cancel(UtcNow);

            if (!await SaveAsync(cancellationToken))
            {
                throw new DomainException(PlatformPaymentsErrors.ChargeOpen);
            }
        }

        PlatformTenantCardProfile profile = await ProfileAsync(tenant, cancellationToken);
        PlatformCardTransaction charge = PlatformCardTransaction.Charge(tenant.Id, invoice.Id, origin, _gateway.Status.Provider, invoice.Balance, invoice.CurrencyCode, null, null, UtcNow);
        _payments.AddTransaction(charge);

        if (!await SaveAsync(cancellationToken))
        {
            throw new DomainException(PlatformPaymentsErrors.ChargeOpen);
        }

        Dictionary<string, string> metadata = Metadata(tenant.Id, invoice.Id, charge.Id, null);

        if (saveCard || autoCharge)
        {
            metadata[PlatformCardMetadata.SaveCard] = "true";
            metadata[PlatformCardMetadata.AddedVia] = (origin == PlatformCardTransactionOrigin.Registration ? PlatformCardAddedVia.Registration : PlatformCardAddedVia.PayLink).ToString();
        }

        if (autoCharge)
        {
            Consent(metadata, tenant);
        }

        PlatformCheckoutSession session = await _gateway.CreateCheckoutAsync(
            new PlatformCheckoutRequest(PlatformCheckoutPurpose.Payment, profile.ProviderCustomerId, charge.MinorUnits, charge.CurrencyCode, invoice.Number, saveCard || autoCharge, metadata, successUrl, cancelUrl, invoice.Language.Value, charge.IdempotencyKey),
            cancellationToken);
        charge.AttachCheckout(session.CheckoutId, UtcNow);
        Audit(null, "platform.payments.checkout_started", null, nameof(PlatformInvoice), invoice.Id, new { number = invoice.Number, origin = origin.ToString(), amount = PlatformPaymentsLimits.Format(charge.Amount), currency = charge.CurrencyCode });
        await SaveAsync(cancellationToken);
        return session;
    }

    public async Task<PlatformCheckoutSession> OpenSaveCardCheckoutAsync(PlatformTenant tenant, PlatformPaymentLink link, bool autoCharge, string successUrl, string cancelUrl, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(tenant);
        ArgumentNullException.ThrowIfNull(link);

        PlatformTenantCardProfile profile = await ProfileAsync(tenant, cancellationToken);
        Dictionary<string, string> metadata = Metadata(tenant.Id, null, null, link.Id);
        metadata[PlatformCardMetadata.SaveCard] = "true";
        metadata[PlatformCardMetadata.AddedVia] = PlatformCardAddedVia.SaveCardLink.ToString();

        if (autoCharge)
        {
            Consent(metadata, tenant);
        }

        PlatformCheckoutSession session = await _gateway.CreateCheckoutAsync(
            new PlatformCheckoutRequest(PlatformCheckoutPurpose.SaveCard, profile.ProviderCustomerId, 0, string.Empty, string.Empty, true, metadata, successUrl, cancelUrl, tenant.DefaultLanguage.Value,
                "vx4-setup-" + link.Id.ToString("N", System.Globalization.CultureInfo.InvariantCulture) + (autoCharge ? "-auto" : "-card")),
            cancellationToken);
        Audit(null, "platform.payments.checkout_started", null, nameof(PlatformTenant), tenant.Id, new { purpose = "save_card" });
        await SaveAsync(cancellationToken);
        return session;
    }

    // =====================================================
    // METHOD: CHARGE A SAVED CARD (D-P4, D-P5)
    // =====================================================
    public async Task ChargeSavedCardAsync(PlatformCardTransaction charge, PlatformTenantCardProfile profile, PlatformSavedCard card, PlatformInvoice invoice, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(charge);
        ArgumentNullException.ThrowIfNull(profile);
        ArgumentNullException.ThrowIfNull(card);
        ArgumentNullException.ThrowIfNull(invoice);

        PlatformCardResult result;

        try
        {
            result = await _gateway.ChargeAsync(
                new PlatformCardChargeRequest(profile.ProviderCustomerId, card.ProviderPaymentMethodId, charge.MinorUnits, charge.CurrencyCode, invoice.Number,
                    Metadata(charge.PlatformTenantId, invoice.Id, charge.Id, null), charge.IdempotencyKey),
                cancellationToken);
        }
        catch (PlatformCardGatewayException exception) when (exception.OutcomeUnknown)
        {
            // Unknown outcome: the clock asks again with the same idempotency key.
            return;
        }
        catch (PlatformCardGatewayException exception)
        {
            result = new PlatformCardResult(null, PlatformCardOutcome.Failed, exception.Code);
        }

        ApplyChargeResult(charge, invoice, result);
    }

    public void ApplyChargeResult(PlatformCardTransaction charge, PlatformInvoice invoice, PlatformCardResult result)
    {
        ArgumentNullException.ThrowIfNull(charge);
        ArgumentNullException.ThrowIfNull(invoice);
        ArgumentNullException.ThrowIfNull(result);

        switch (result.Outcome)
        {
            case PlatformCardOutcome.Pending:
            case PlatformCardOutcome.Succeeded:
                // Only the webhook (or a provider lookup by the clock) records the payment (D-P5).
                charge.MarkPending(result.ProviderId, UtcNow);
                break;
            case PlatformCardOutcome.RequiresAction:
                if (charge.MarkRequiresAction(result.ProviderId, UtcNow))
                {
                    Problem(charge, invoice, true);
                }

                break;
            case PlatformCardOutcome.Failed:
                if (charge.Fail(result.ProviderId, result.FailureCode, UtcNow))
                {
                    Problem(charge, invoice, false);
                }

                break;
            default:
                charge.Cancel(UtcNow);
                break;
        }
    }

    // Staff see it in the history, the tenant gets an email with a pay link, Slice 6 decides about retries.
    public void Problem(PlatformCardTransaction charge, PlatformInvoice invoice, bool actionRequired)
    {
        ArgumentNullException.ThrowIfNull(charge);
        ArgumentNullException.ThrowIfNull(invoice);

        _billing.Invoices.AddEvents(new[]
        {
            invoice.RecordCardAttempt(actionRequired ? PlatformInvoiceEventCodes.CardActionRequired : PlatformInvoiceEventCodes.CardFailed, charge.Amount, charge.FailureCode, UtcNow)
        });
        Audit(null, actionRequired ? "platform.payments.card_action_required" : "platform.payments.card_charge_failed", null, nameof(PlatformInvoice), invoice.Id,
            new { number = invoice.Number, origin = charge.Origin.ToString(), code = charge.FailureCode });
        _billing.Outbox.Write(
            PlatformMessageTypes.CardPaymentFailed,
            charge.PlatformTenantId,
            new PlatformCardPaymentFailed(invoice.Id, charge.PlatformTenantId, charge.Id, charge.Origin, charge.FailureCode, actionRequired, !actionRequired && !Final(charge.FailureCode), UtcNow));
    }

    // =====================================================
    // METHOD: RETRIES (Slice 6, D-C9)
    // =====================================================
    // A retry needs consent, a valid default card, no open charge, and a last failure that was not final.
    public async Task<PlatformCardRetryPlan?> RetryPlanAsync(PlatformInvoice invoice, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(invoice);

        if (invoice.Status != PlatformInvoiceStatus.Issued || invoice.Balance <= 0m
            || await RefusalForAsync(invoice.CurrencyCode, cancellationToken) is not null
            || await _payments.GetOpenChargeAsync(invoice.Id, cancellationToken) is not null
            || await _payments.GetProfileAsync(invoice.PlatformTenantId, cancellationToken) is not { AutoCharge: true } profile
            || !profile.Matches(_gateway.Status.Provider, _gateway.Status.Mode ?? PlatformPaymentMode.Test)
            || await DefaultCardAsync(invoice.PlatformTenantId, cancellationToken) is not { } card
            || card.IsExpiredAt(UtcNow))
        {
            return null;
        }

        PlatformCardTransaction? last = (await _payments.ListForInvoiceAsync(invoice.Id, cancellationToken))
            .Where(item => item.Kind == PlatformCardTransactionKind.Charge)
            .OrderByDescending(item => item.CreatedUtc)
            .FirstOrDefault();

        return last is { Status: PlatformCardTransactionStatus.Failed } && !Final(last.FailureCode)
            ? new PlatformCardRetryPlan(profile, card)
            : null;
    }

    public async Task RetryChargeAsync(PlatformInvoice invoice, PlatformTenantCardProfile profile, PlatformSavedCard card, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(invoice);
        ArgumentNullException.ThrowIfNull(profile);
        ArgumentNullException.ThrowIfNull(card);

        PlatformCardTransaction charge = PlatformCardTransaction.Charge(invoice.PlatformTenantId, invoice.Id, PlatformCardTransactionOrigin.Retry, _gateway.Status.Provider, invoice.Balance, invoice.CurrencyCode, card.Id, null, UtcNow);
        _payments.AddTransaction(charge);

        // Recorded before the provider is asked (one open charge per invoice), as for the automatic charge.
        if (!await SaveAsync(cancellationToken))
        {
            return;
        }

        await ChargeSavedCardAsync(charge, profile, card, invoice, cancellationToken);
        await SaveAsync(cancellationToken);
    }

    private static bool Final(string? failureCode)
    {
        return failureCode is "expired_card" or "incorrect_number" or "invalid_account" or "card_not_supported" or "currency_not_supported" or "stolen_card" or "lost_card" or "pickup_card";
    }

    private void Consent(Dictionary<string, string> metadata, PlatformTenant tenant)
    {
        metadata[PlatformCardMetadata.AutoCharge] = "true";
        metadata[PlatformCardMetadata.ConsentEmail] = tenant.ContactFor(PlatformTenantContactRole.Billing).Email.Value;
        metadata[PlatformCardMetadata.ConsentVersion] = ConsentTextVersion;
    }

    public static Dictionary<string, string> Metadata(Guid tenantId, Guid? invoiceId, Guid? transactionId, Guid? linkId)
    {
        Dictionary<string, string> metadata = new(StringComparer.Ordinal) { [PlatformCardMetadata.Tenant] = tenantId.ToString("D") };

        if (invoiceId is Guid invoice)
        {
            metadata[PlatformCardMetadata.Invoice] = invoice.ToString("D");
        }

        if (transactionId is Guid transaction)
        {
            metadata[PlatformCardMetadata.Transaction] = transaction.ToString("D");
        }

        if (linkId is Guid link)
        {
            metadata[PlatformCardMetadata.Link] = link.ToString("D");
        }

        return metadata;
    }
}
