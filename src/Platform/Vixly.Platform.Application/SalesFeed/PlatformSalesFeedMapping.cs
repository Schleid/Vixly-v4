using System.Globalization;
using Vixly.Platform.Domain.Billing;
using Vixly.Platform.Domain.Subscriptions;

namespace Vixly.Platform.Application.SalesFeed;

// =========================================================
// FILE: PlatformSalesFeedMapping.cs
//
// PURPOSE:
// Turns Vixly's own values into the contract's words, amounts and dates.
//
// RESPONSIBILITIES:
// - Map subscription statuses, terms and history codes; payment methods; reversal reasons
// - Write amounts as strings with exactly 2 decimals and dates as ISO 8601
// - List the history codes the feed sends
//
// IMPORTANT NOTES:
// - Contract values agreed with TechOS (version 1); a change here is a contract change
// - Statuses map one to one; a cancellation at period end stays "active" until it ends
// - A subscription history code with no contract change word is not sent
// =========================================================
public static class PlatformSalesFeedMapping
{
    public static IReadOnlyList<string> InvoiceCodes { get; } = new[]
    {
        PlatformInvoiceEventCodes.Issued,
        PlatformInvoiceEventCodes.PaymentRecorded,
        PlatformInvoiceEventCodes.PaymentReversed,
        PlatformInvoiceEventCodes.CardRefunded,
        PlatformInvoiceEventCodes.DisputeLost,
        PlatformInvoiceEventCodes.Voided,
        PlatformInvoiceEventCodes.CreditIssued
    };

    public static IReadOnlyList<string> SubscriptionCodes { get; } = new[]
    {
        PlatformSubscriptionEventCodes.StartedTrial,
        PlatformSubscriptionEventCodes.StartedAwaitingPayment,
        PlatformSubscriptionEventCodes.Activated,
        PlatformSubscriptionEventCodes.ActivatedNothingDue,
        PlatformSubscriptionEventCodes.OfflinePaymentRecorded,
        PlatformSubscriptionEventCodes.Renewed,
        PlatformSubscriptionEventCodes.Changed,
        PlatformSubscriptionEventCodes.SuspendedWithTenant,
        PlatformSubscriptionEventCodes.BlockedForNonPayment,
        PlatformSubscriptionEventCodes.RestoredWithTenant,
        PlatformSubscriptionEventCodes.BlockLifted,
        PlatformSubscriptionEventCodes.InvoicePaid,
        PlatformSubscriptionEventCodes.Resumed,
        PlatformSubscriptionEventCodes.Ended
    };

    public static string Type(string invoiceEventCode)
    {
        return invoiceEventCode switch
        {
            PlatformInvoiceEventCodes.Issued => PlatformSalesFeedTypes.InvoiceIssued,
            PlatformInvoiceEventCodes.PaymentRecorded => PlatformSalesFeedTypes.PaymentReceived,
            PlatformInvoiceEventCodes.PaymentReversed or PlatformInvoiceEventCodes.CardRefunded or PlatformInvoiceEventCodes.DisputeLost => PlatformSalesFeedTypes.PaymentReversed,
            PlatformInvoiceEventCodes.Voided => PlatformSalesFeedTypes.InvoiceVoided,
            PlatformInvoiceEventCodes.CreditIssued => PlatformSalesFeedTypes.InvoiceCredited,
            _ => throw new ArgumentOutOfRangeException(nameof(invoiceEventCode), invoiceEventCode, "Not a sales feed code.")
        };
    }

    public static string Status(PlatformSubscriptionStatus status)
    {
        return status switch
        {
            PlatformSubscriptionStatus.Trialing => "trial",
            PlatformSubscriptionStatus.PendingPayment => "pending_payment",
            PlatformSubscriptionStatus.Active => "active",
            PlatformSubscriptionStatus.PastDue => "past_due",
            PlatformSubscriptionStatus.Suspended => "suspended",
            PlatformSubscriptionStatus.BlockedForNonPayment => "blocked",
            PlatformSubscriptionStatus.Ended => "ended",
            _ => throw new ArgumentOutOfRangeException(nameof(status), status, "Unknown subscription status.")
        };
    }

    public static string Term(PlatformSubscriptionTerm term)
    {
        return term == PlatformSubscriptionTerm.Annual ? "annual" : "monthly";
    }

    // The contract's change word; a plan change says whether the plan (or term) or only the licences changed.
    public static string? Change(string eventCode, IReadOnlyDictionary<string, string> arguments)
    {
        ArgumentNullException.ThrowIfNull(arguments);

        return eventCode switch
        {
            PlatformSubscriptionEventCodes.StartedTrial
                or PlatformSubscriptionEventCodes.StartedAwaitingPayment
                or PlatformSubscriptionEventCodes.Activated
                or PlatformSubscriptionEventCodes.ActivatedNothingDue
                or PlatformSubscriptionEventCodes.OfflinePaymentRecorded => "started",
            PlatformSubscriptionEventCodes.Renewed => "renewed",
            PlatformSubscriptionEventCodes.Changed => OnlyLicences(arguments) ? "licences_changed" : "plan_changed",
            PlatformSubscriptionEventCodes.SuspendedWithTenant => "suspended",
            PlatformSubscriptionEventCodes.BlockedForNonPayment => "blocked",
            PlatformSubscriptionEventCodes.RestoredWithTenant
                or PlatformSubscriptionEventCodes.BlockLifted
                or PlatformSubscriptionEventCodes.InvoicePaid
                or PlatformSubscriptionEventCodes.Resumed => "reactivated",
            PlatformSubscriptionEventCodes.Ended => "ended",
            _ => null
        };
    }

    // card, wallet (Apple Pay, Google Pay), us_bank, zelle; everything staff record is manual.
    public static string Method(PlatformPaymentMethod method, string? wallet)
    {
        return method switch
        {
            PlatformPaymentMethod.Card => wallet is null ? "card" : "wallet",
            PlatformPaymentMethod.BankDebit => "us_bank",
            PlatformPaymentMethod.Zelle => "zelle",
            _ => "manual"
        };
    }

    public static string Reason(string invoiceEventCode, PlatformPaymentMethod method)
    {
        return invoiceEventCode switch
        {
            PlatformInvoiceEventCodes.DisputeLost => "chargeback",
            PlatformInvoiceEventCodes.PaymentReversed when method == PlatformPaymentMethod.BankDebit => "bank_failed",
            _ => "refund"
        };
    }

    public static string Money(decimal amount)
    {
        return Math.Round(Math.Abs(amount), 2, MidpointRounding.AwayFromZero).ToString("0.00", CultureInfo.InvariantCulture);
    }

    public static string Date(DateTimeOffset value)
    {
        return value.UtcDateTime.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
    }

    public static string Date(DateOnly value)
    {
        return value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
    }

    public static string Utc(DateTimeOffset value)
    {
        return value.UtcDateTime.ToString("yyyy-MM-dd'T'HH:mm:ss.fff'Z'", CultureInfo.InvariantCulture);
    }

    private static bool OnlyLicences(IReadOnlyDictionary<string, string> arguments)
    {
        return arguments.TryGetValue("fromPlan", out string? fromPlan) && arguments.TryGetValue("plan", out string? plan) && fromPlan == plan
            && arguments.TryGetValue("fromTerm", out string? fromTerm) && arguments.TryGetValue("term", out string? term) && fromTerm == term
            && arguments.TryGetValue("fromLicences", out string? fromLicences) && arguments.TryGetValue("licences", out string? licences) && fromLicences != licences;
    }
}
