using System.Text.Json;
using Vixly.Contracts.Platform.Common;
using Vixly.Contracts.Platform.Events;
using Vixly.Contracts.Platform.Internal.Billing;
using Vixly.Contracts.Platform.Internal.Devices;
using Vixly.Contracts.Platform.Internal.Entitlements;
using Vixly.Contracts.Platform.Internal.Legal;
using Vixly.Contracts.Platform.Internal.Portal;
using Vixly.Contracts.Platform.Internal.Subscription;
using Vixly.Contracts.Platform.Internal.Tenant;
using Vixly.Platform.Application.Licensing;
using Vixly.Platform.Application.Messaging;
using Vixly.Platform.Application.Queries;
using Vixly.Platform.Application.UseCases.Legal;
using Vixly.Platform.Application.UseCases.Subscriptions;
using Vixly.Platform.Application.UseCases.TenantAccount;
using Vixly.Platform.Domain.Messaging;
using Vixly.Shared.Kernel.Paging;

namespace Vixly.Platform.Api.Endpoints.Internal;

// =========================================================
// FILE: PlatformInternalMapping.cs
//
// PURPOSE:
// Turns use-case answers into Contracts.Platform wire records (Slice 9, D-K10).
//
// RESPONSIBILITIES:
// - Map every internal answer
// - Write enum values as stable wire codes
//
// IMPORTANT NOTES:
// - No domain type leaves Platform.Api
// - File links are relative to the internal base address
// =========================================================
public static class PlatformInternalMapping
{
    public static PlatformPortalAccessInfo Portal(PlatformPortalAccess access)
    {
        ArgumentNullException.ThrowIfNull(access);

        return new PlatformPortalAccessInfo(
            access.Found,
            access.PlatformTenantId,
            PlatformWireCodes.Of(access.Status),
            PlatformWireCodes.Of(access.Access),
            access.CanonicalSlug,
            access.DefaultLanguage,
            PlatformWireCodes.Of(access.Legal));
    }

    public static PlatformEntitlementsInfo Entitlements(PlatformTenantEntitlements entitlements)
    {
        ArgumentNullException.ThrowIfNull(entitlements);

        return new PlatformEntitlementsInfo(
            entitlements.PlatformTenantId,
            PlatformWireCodes.Of(entitlements.Status),
            PlatformWireCodes.Of(entitlements.Access),
            entitlements.Features.Select(feature => PlatformWireCodes.Of(feature)).ToArray(),
            entitlements.Limits.ToDictionary(item => PlatformWireCodes.Of(item.Key), item => item.Value, StringComparer.Ordinal),
            entitlements.NonPayment is { } due
                ? new PlatformNonPaymentPartInfo(due.BlockedUtc, due.AmountDue.Select(item => new PlatformMoneyInfo(item.CurrencyCode, item.Amount)).ToArray(), due.CardPaymentsAvailable)
                : null,
            entitlements.Legal is { } legal ? new PlatformEntitlementsLegalInfo(PlatformWireCodes.Of(legal.Status), legal.DeadlineUtc, legal.RequiredVersion) : null);
    }

    public static PlatformTenantAccountInfo Account(PlatformTenantAccountSummary account)
    {
        ArgumentNullException.ThrowIfNull(account);

        return new PlatformTenantAccountInfo(
            account.PlatformTenantId,
            account.TenantCode,
            account.Slug,
            account.LegalName,
            account.TradingName,
            account.Email,
            account.Phone,
            new PlatformPostalAddressInfo(account.Address.Line1, account.Address.Line2, account.Address.City, account.Address.StateProvince, account.Address.PostalCode, account.Address.CountryCode),
            account.DefaultLanguage,
            account.TimeZone,
            PlatformWireCodes.Of(account.Status),
            account.Contacts.Select(contact => new PlatformTenantContactInfo(PlatformWireCodes.Of(contact.Role), contact.Name, contact.Email, contact.Phone)).ToArray(),
            account.Support is { } support ? new PlatformSupportInfo(support.CompanyName, support.Email, support.Phone, support.Website) : null);
    }

    public static PlatformCompanyChangeInfo CompanyChange(PlatformTenantChangeItem change)
    {
        ArgumentNullException.ThrowIfNull(change);

        return new PlatformCompanyChangeInfo(
            change.RequestId,
            PlatformWireCodes.Of(change.Status),
            Company(change.Previous),
            Company(change.Proposed),
            change.Reason,
            change.SubmittedByName,
            change.SubmittedUtc,
            change.DecidedUtc,
            change.DecisionNote);
    }

    public static PlatformCompanyDetailsInfo Company(PlatformTenantCompanyItem company)
    {
        ArgumentNullException.ThrowIfNull(company);

        return new PlatformCompanyDetailsInfo(
            company.LegalName,
            company.TradingName,
            company.Email,
            company.Phone,
            new PlatformPostalAddressInfo(company.AddressLine1, company.AddressLine2, company.City, company.StateProvince, company.PostalCode, company.CountryCode));
    }

    public static PlatformSubscriptionInfo Subscription(PlatformTenantSubscriptionView view)
    {
        ArgumentNullException.ThrowIfNull(view);

        return new PlatformSubscriptionInfo(
            view.PlanCode,
            view.PlanName,
            view.VersionNumber,
            PlatformWireCodes.Of(view.Status),
            PlatformWireCodes.Of(view.Term),
            view.Licences,
            view.ActiveLicences,
            view.PricePerLicence,
            view.PeriodAmount,
            view.NextPeriodNetAmount,
            view.CurrencyCode,
            view.TrialEndsUtc,
            view.PeriodStartUtc,
            view.PeriodEndUtc,
            view.CancelAtPeriodEnd,
            PlatformWireCodes.Of(view.Access),
            view.ScheduledChange is { } change
                ? new PlatformScheduledChangeInfo(change.PlanCode, change.PlanName, PlatformWireCodes.Of(change.Term), change.Licences, change.EffectiveUtc, change.PeriodAmount)
                : null,
            view.Limits.ToDictionary(item => PlatformWireCodes.Of(item.Key), item => item.Value, StringComparer.Ordinal),
            view.Features.Select(feature => PlatformWireCodes.Of(feature)).ToArray());
    }

    public static PlatformPlanOptionInfo Plan(PlatformPlanOption option)
    {
        ArgumentNullException.ThrowIfNull(option);

        return new PlatformPlanOptionInfo(option.PlatformPlanId, option.Code, option.Name, option.CurrencyCode, option.MonthlyPricePerLicence, option.AnnualPricePerLicence,
            option.SetupFee, option.MinimumLicences, option.MaxLicences, option.TrialDays);
    }

    public static PlatformSubscriptionQuoteInfo Quote(PlatformSubscriptionQuoteItem quote)
    {
        ArgumentNullException.ThrowIfNull(quote);

        return new PlatformSubscriptionQuoteInfo(PlatformWireCodes.Of(quote.Timing), quote.PlanCode, PlatformWireCodes.Of(quote.Term), quote.Licences, quote.ChargeNow,
            quote.NextPeriodAmount, quote.NextPeriodNetAmount, quote.CurrencyCode, quote.EffectiveUtc, quote.EndsTrial);
    }

    public static PlatformInvoiceInfo Invoice(PlatformInvoiceListItem invoice)
    {
        ArgumentNullException.ThrowIfNull(invoice);

        return new PlatformInvoiceInfo(invoice.PlatformInvoiceId, invoice.Number, PlatformWireCodes.Of(invoice.Kind), PlatformWireCodes.Of(invoice.Status), invoice.CurrencyCode,
            invoice.Total, invoice.Balance, invoice.PaidTotal, invoice.IssuedUtc, invoice.DueUtc, invoice.Overdue, invoice.CreditPending);
    }

    public static PlatformBillingDocumentInfo Document(PlatformBillingDocumentItem document)
    {
        ArgumentNullException.ThrowIfNull(document);

        string kind = PlatformWireCodes.Of(document.Kind);
        return new PlatformBillingDocumentInfo(kind, document.OwnerId, document.Number, document.PlatformInvoiceId, document.InvoiceNumber, document.Language, document.CreatedUtc,
            $"/internal/v1/tenant/documents/{kind}/{document.OwnerId:D}");
    }

    public static PlatformDeviceInfo Device(PlatformDeviceItem device)
    {
        ArgumentNullException.ThrowIfNull(device);

        return new PlatformDeviceInfo(device.DeviceId, device.Number, device.Name, PlatformWireCodes.Of(device.Kind), PlatformWireCodes.Of(device.Proof), PlatformWireCodes.Of(device.Status),
            device.SeatHeld, PlatformWireCodes.Of(device.Reason), device.RequestedByName, device.RequestedUtc, device.ApprovedUtc, device.ReleaseRequestedUtc, device.EndedUtc, device.LastSeenUtc, device.Idle);
    }

    public static PlatformDeviceEnrolmentInfo Enrolment(PlatformDeviceEnrolment enrolment)
    {
        ArgumentNullException.ThrowIfNull(enrolment);

        return new PlatformDeviceEnrolmentInfo(enrolment.DeviceId, enrolment.Number, PlatformWireCodes.Of(enrolment.Status), enrolment.Secret);
    }

    public static PlatformDeviceCheckInfo Check(PlatformDeviceCheck check)
    {
        ArgumentNullException.ThrowIfNull(check);

        return new PlatformDeviceCheckInfo(check.Allowed, check.Code, PlatformWireCodes.Of(check.Access), check.NewSecret);
    }

    public static PlatformLegalStatusInfo Legal(PlatformLegalStatusAnswer answer)
    {
        ArgumentNullException.ThrowIfNull(answer);

        return new PlatformLegalStatusInfo(PlatformWireCodes.Of(answer.Status), answer.DeadlineUtc, answer.ReadOnly,
            answer.Terms is { } terms ? PlatformLegalEndpoints.Info(terms) : null);
    }

    public static PlatformEventInfo Event(PlatformPublishedEvent item)
    {
        ArgumentNullException.ThrowIfNull(item);

        using JsonDocument data = JsonDocument.Parse(item.Payload);
        return new PlatformEventInfo(item.Sequence, item.Type, item.PlatformTenantId, item.OccurredUtc, data.RootElement.Clone());
    }

    public static PlatformPageInfo<TOut> Page<TIn, TOut>(PagedResult<TIn> page, Func<TIn, TOut> map)
    {
        ArgumentNullException.ThrowIfNull(page);
        ArgumentNullException.ThrowIfNull(map);

        return new PlatformPageInfo<TOut>(page.Items.Select(map).ToArray(), page.Page, page.PageSize, page.TotalCount);
    }
}
