namespace Vixly.Platform.Application.Messaging;

// =========================================================
// FILE: PlatformMessageTypes.cs
//
// PURPOSE:
// The versioned types of outbox messages (D-T17).
//
// RESPONSIBILITIES:
// - Name every message the Platform writes to its outbox
//
// IMPORTANT NOTES:
// - A type never changes meaning; a new shape is a new version
// =========================================================
public static class PlatformMessageTypes
{
    public const string TenantStatusChanged = "platform.tenant.status_changed.v1";

    public const string TenantSlugCorrected = "platform.tenant.slug_corrected.v1";

    public const string SubscriptionStarted = "platform.subscription.started.v1";

    public const string SubscriptionChanged = "platform.subscription.changed.v1";

    public const string SubscriptionChangeScheduled = "platform.subscription.change_scheduled.v1";

    public const string SubscriptionRenewed = "platform.subscription.renewed.v1";

    public const string SubscriptionStatusChanged = "platform.subscription.status_changed.v1";

    public const string SubscriptionCancellationRequested = "platform.subscription.cancellation_requested.v1";

    public const string SubscriptionTrialExtended = "platform.subscription.trial_extended.v1";

    public const string SubscriptionEnded = "platform.subscription.ended.v1";

    public const string PlanVersionPublished = "platform.plan.version_published.v1";
    public const string SubscriptionDiscountChanged = "platform.subscription.discount_changed.v1";
    public const string SubscriptionChargeChanged = "platform.subscription.charge_changed.v1";
    public const string ApprovalRequested = "platform.sales.approval_requested.v1";
    public const string DiscountEnding = "platform.subscription.discount_ending.v1";
    public const string BillingDocumentIssued = "platform.billing.document_issued.v1";
    public const string InvoicePaid = "platform.billing.invoice_paid.v1";

    public const string ProviderEvent = "platform.payments.provider_event.v1";

    public const string CardPaymentFailed = "platform.payments.card_payment_failed.v1";

    public const string CollectionsNotice = "platform.collections.notice.v1";

    public const string LicensingNotice = "platform.licensing.notice.v1";

    public const string ZelleNotice = "platform.payments.zelle_notice.v1";

    public const string LegalNotice = "platform.legal.notice.v1";

    public const string SalesLeadReceived = "platform.sales.lead_received.v1";

    public const string TenantCompanyChangeSubmitted = "platform.tenant.company_change_submitted.v1";

    public const string TenantCompanyChangeDecided = "platform.tenant.company_change_decided.v1";

    // For Operations (contract, D-Lg11).
    public const string LegalVersionPublished = "platform.legal.version_published.v1";

    public const string TermsAccepted = "platform.legal.terms_accepted.v1";

    // Operations Slice 1: a registration is ready for the owner to set up the account (ids only).
    public const string RegistrationAccountSetupReady = "platform.registration.account_setup_ready.v1";

    // For Operations (contract, D-L14): drop cached device checks on these.
    public const string DeviceApproved = "platform.device.approved.v1";

    public const string DeviceReleased = "platform.device.released.v1";

    public const string DeviceRevoked = "platform.device.revoked.v1";
}
