using System.Globalization;
using Vixly.Contracts.Platform.Common;
using Vixly.Contracts.Platform.Internal.Billing;
using Vixly.Contracts.Platform.Internal.Subscription;
using Vixly.Contracts.Platform.Internal.Tenant;
using Vixly.Operations.Application.Company;
using Vixly.Operations.Application.Ports;
using Vixly.Operations.Application.Security;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Infrastructure.Platform;

// =========================================================
// FILE: OperationsCompanyClient.cs
//
// PURPOSE:
// Calls Platform for the company's account, change requests and billing (Slice 1E).
//
// RESPONSIBILITIES:
// - Map Contracts.Platform answers to Operations records
// - Translate Platform refusals to ops.* codes
//
// IMPORTANT NOTES:
// - Every call is signed for the acting user (role and step-up)
// =========================================================
public sealed class OperationsCompanyClient : IOperationsCompanyClient
{
    private readonly OperationsPlatformSender _sender;

    public OperationsCompanyClient(OperationsPlatformSender sender)
    {
        _sender = sender ?? throw new ArgumentNullException(nameof(sender));
    }

    public async Task<OperationsPlatformCall<OperationsCompany>> GetAsync(OperationsPlatformActor actor, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(actor);
        (PlatformTenantAccountInfo? info, string? refused) = await _sender.SendAsync<PlatformTenantAccountInfo>(HttpMethod.Get, "internal/v1/tenant", actor.TenantId, actor, null, cancellationToken);

        if (info is null)
        {
            return OperationsPlatformCall<OperationsCompany>.Refused(Translate(refused ?? string.Empty));
        }

        return OperationsPlatformCall<OperationsCompany>.Ok(new OperationsCompany(
            info.TenantCode,
            new OperationsCompanyDetails(info.LegalName, info.TradingName, info.Email, info.Phone, Address(info.Address)),
            info.DefaultLanguage,
            info.TimeZone,
            info.Contacts.Select(contact => new OperationsCompanyContact(contact.Role, contact.Name, contact.Email, contact.Phone)).ToList()));
    }

    public async Task<OperationsPlatformCall<bool>> UpdateAsync(OperationsPlatformActor actor, IReadOnlyList<OperationsCompanyContact> contacts, string language, string timeZone, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(actor);
        ArgumentNullException.ThrowIfNull(contacts);
        UpdatePlatformTenantAccountBody body = new(contacts.Select(contact => new PlatformTenantContactBody(contact.Role, contact.Name, contact.Email, contact.Phone)).ToList(), language, timeZone);
        (_, string? refused) = await _sender.SendAsync<object>(HttpMethod.Put, "internal/v1/tenant", actor.TenantId, actor, body, cancellationToken);

        return refused is null ? OperationsPlatformCall<bool>.Ok(true) : OperationsPlatformCall<bool>.Refused(Translate(refused));
    }

    public async Task<OperationsCompanyChange?> GetChangeAsync(OperationsPlatformActor actor, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(actor);
        (PlatformCompanyChangeInfo? info, _) = await _sender.SendAsync<PlatformCompanyChangeInfo>(HttpMethod.Get, "internal/v1/tenant/company-change", actor.TenantId, actor, null, cancellationToken);

        return info is null
            ? null
            : new OperationsCompanyChange(info.RequestId, info.Status, Details(info.Proposed), info.Reason, info.SubmittedByName, info.SubmittedUtc, info.DecidedUtc, info.DecisionNote);
    }

    public async Task<OperationsPlatformCall<bool>> SubmitChangeAsync(OperationsPlatformActor actor, OperationsCompanyChangeInput input, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(actor);
        ArgumentNullException.ThrowIfNull(input);
        PlatformCompanyChangeBody body = new()
        {
            LegalName = input.LegalName,
            TradingName = input.TradingName,
            Email = input.Email,
            Phone = input.Phone,
            AddressLine1 = input.AddressLine1,
            AddressLine2 = input.AddressLine2,
            City = input.City,
            StateProvince = input.StateProvince,
            PostalCode = input.PostalCode,
            CountryCode = input.CountryCode,
            Reason = input.Reason,
            ProofFileName = input.ProofFileName,
            ProofBase64 = input.ProofContent is { Length: > 0 } content ? Convert.ToBase64String(content) : null
        };
        (_, string? refused) = await _sender.SendAsync<object>(HttpMethod.Post, "internal/v1/tenant/company-change", actor.TenantId, actor, body, cancellationToken);

        return refused is null ? OperationsPlatformCall<bool>.Ok(true) : OperationsPlatformCall<bool>.Refused(Translate(refused));
    }

    public async Task<OperationsPlatformCall<bool>> WithdrawChangeAsync(OperationsPlatformActor actor, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(actor);
        (_, string? refused) = await _sender.SendAsync<object>(HttpMethod.Delete, "internal/v1/tenant/company-change", actor.TenantId, actor, null, cancellationToken);

        return refused is null ? OperationsPlatformCall<bool>.Ok(true) : OperationsPlatformCall<bool>.Refused(Translate(refused));
    }

    public async Task<OperationsSubscription?> GetSubscriptionAsync(OperationsPlatformActor actor, string language, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(actor);
        (PlatformSubscriptionInfo? info, _) = await _sender.SendAsync<PlatformSubscriptionInfo>(
            HttpMethod.Get, "internal/v1/tenant/subscription?language=" + Uri.EscapeDataString(language), actor.TenantId, actor, null, cancellationToken);

        return info is null
            ? null
            : new OperationsSubscription(info.PlanName, info.Status, info.Term, info.Licences, info.ActiveLicences, info.PeriodAmount, info.CurrencyCode, info.TrialEndsUtc, info.PeriodEndUtc, info.CancelAtPeriodEnd);
    }

    public async Task<(IReadOnlyList<OperationsInvoice> Items, int TotalCount)> ListInvoicesAsync(OperationsPlatformActor actor, int page, int pageSize, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(actor);
        (PlatformPageInfo<PlatformInvoiceInfo>? info, _) = await _sender.SendAsync<PlatformPageInfo<PlatformInvoiceInfo>>(
            HttpMethod.Get, string.Create(CultureInfo.InvariantCulture, $"internal/v1/tenant/invoices?page={page}&pageSize={pageSize}"), actor.TenantId, actor, null, cancellationToken);

        return info is null
            ? (Array.Empty<OperationsInvoice>(), 0)
            : (info.Items.Select(item => new OperationsInvoice(item.InvoiceId, item.Number, item.Status, item.CurrencyCode, item.Total, item.Balance, item.IssuedUtc, item.DueUtc, item.Overdue, item.CreditPending)).ToList(), info.TotalCount);
    }

    public async Task<(IReadOnlyList<OperationsBillingDocument> Items, int TotalCount)> ListDocumentsAsync(OperationsPlatformActor actor, int page, int pageSize, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(actor);
        (PlatformPageInfo<PlatformBillingDocumentInfo>? info, _) = await _sender.SendAsync<PlatformPageInfo<PlatformBillingDocumentInfo>>(
            HttpMethod.Get, string.Create(CultureInfo.InvariantCulture, $"internal/v1/tenant/documents?page={page}&pageSize={pageSize}"), actor.TenantId, actor, null, cancellationToken);

        return info is null
            ? (Array.Empty<OperationsBillingDocument>(), 0)
            : (info.Items.Select(item => new OperationsBillingDocument(item.Kind, item.DocumentId, item.Number, item.InvoiceNumber, item.Language, item.CreatedUtc)).ToList(), info.TotalCount);
    }

    public Task<OperationsLegalFile?> GetDocumentAsync(OperationsPlatformActor actor, string kind, Guid documentId, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(actor);
        return _sender.SendFileAsync($"internal/v1/tenant/documents/{Uri.EscapeDataString(kind)}/{documentId:D}", actor.TenantId, actor, cancellationToken);
    }

    public async Task<OperationsPlatformCall<string>> CreatePayLinkAsync(OperationsPlatformActor actor, Guid invoiceId, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(actor);
        (PlatformPayLinkInfo? link, string? refused) = await _sender.SendAsync<PlatformPayLinkInfo>(
            HttpMethod.Post, $"internal/v1/tenant/invoices/{invoiceId:D}/pay-link", actor.TenantId, actor, null, cancellationToken);

        // Only an https address from Platform is ever sent to the browser.
        return link is not null && Uri.TryCreate(link.Url, UriKind.Absolute, out Uri? url) && url.Scheme == Uri.UriSchemeHttps
            ? OperationsPlatformCall<string>.Ok(url.AbsoluteUri)
            : OperationsPlatformCall<string>.Refused(refused is null ? OperationsCompanyErrors.BillingRefused : Translate(refused));
    }

    public static Error Translate(string code) => code switch
    {
        "platform.common.step_up_required" => OperationsAccessErrors.StepUpRequired,
        "platform.common.role_not_allowed" => OperationsAccessErrors.PermissionRequired,
        "platform.tenant_changes.already_pending" => OperationsCompanyErrors.ChangePending,
        "platform.tenant_changes.no_difference" => OperationsCompanyErrors.NoDifference,
        "platform.tenant_changes.proof_required" => OperationsCompanyErrors.ProofRequired,
        "platform.tenant_changes.proof_too_large" => OperationsCompanyErrors.ProofTooLarge,
        "platform.tenant_changes.proof_type_invalid" => OperationsCompanyErrors.ProofTypeInvalid,
        "platform.tenant_changes.reason_required" or "platform.tenant_changes.reason_too_long" => OperationsCompanyErrors.ReasonRequired,
        "platform.tenant_changes.not_found" or "platform.tenant_changes.not_pending" => OperationsCompanyErrors.NoChange,
        "platform.tenants.time_zone_invalid" => OperationsCompanyErrors.TimeZoneInvalid,
        "platform.tenants.contact_name_required" or "platform.tenants.contact_name_too_long" or "platform.tenants.contact_role_duplicate"
            or "platform.tenants.contact_role_invalid" or "platform.tenant_account.owner_contact_locked" => OperationsCompanyErrors.ContactInvalid,
        "platform.billing.invoice_paid" or "platform.billing.invoice_void" or "platform.billing.invoice_not_found" or "platform.billing.method_not_allowed"
            or "platform.billing.credit_pending" => OperationsCompanyErrors.InvoiceNotPayable,
        _ when code.StartsWith("platform.tenants.", StringComparison.Ordinal) || code == "platform.common.request_invalid" => OperationsCompanyErrors.DetailsInvalid,
        _ when code.StartsWith("platform.billing.", StringComparison.Ordinal) => OperationsCompanyErrors.BillingRefused,
        _ => OperationsCompanyErrors.Refused
    };

    private static OperationsCompanyAddress Address(PlatformPostalAddressInfo address) =>
        new(address.Line1, address.Line2, address.City, address.StateProvince, address.PostalCode, address.CountryCode);

    private static OperationsCompanyDetails Details(PlatformCompanyDetailsInfo details) =>
        new(details.LegalName, details.TradingName, details.Email, details.Phone, Address(details.Address));
}
