namespace Vixly.Platform.Application.Audit;

// =========================================================
// FILE: PlatformAuditActions.cs
//
// PURPOSE:
// The catalogue of audit action codes the Platform writes.
//
// RESPONSIBILITIES:
// - List every action code, for the audit log filter and its text
//
// IMPORTANT NOTES:
// - An architecture test proves every code written by a use case
//   is listed here and has text in en, fr and es
// - Never rename a shipped code: audit rows store it
// =========================================================
public static class PlatformAuditActions
{
    public static IReadOnlyList<string> Catalogue { get; } = new[]
    {
        "platform.setup.first_super_user_invited",
        "platform.auth.signed_in",
        "platform.auth.signed_out",
        "platform.auth.sign_in_refused",
        "platform.auth.password_failed",
        "platform.auth.code_failed",
        "platform.auth.recovery_code_failed",
        "platform.auth.passkey_failed",
        "platform.auth.step_up_confirmed",
        "platform.auth.step_up_failed",
        "platform.auth.invitation_accepted",
        "platform.auth.password_reset_requested",
        "platform.auth.password_reset",
        "platform.account.password_changed",
        "platform.account.language_changed",
        "platform.account.passkey_added",
        "platform.account.passkey_removed",
        "platform.account.recovery_codes_replaced",
        "platform.account.sessions_ended",
        "platform.users.invited",
        "platform.users.invitation_resent",
        "platform.users.password_reset_sent",
        "platform.users.locked",
        "platform.users.suspended",
        "platform.users.deactivated",
        "platform.users.reactivated",
        "platform.users.second_factor_reset",
        "platform.users.sessions_ended",
        "platform.roles.system_created",
        "platform.roles.system_corrected",
        "platform.roles.created",
        "platform.roles.renamed",
        "platform.roles.deactivated",
        "platform.roles.reactivated",
        "platform.roles.permissions_changed",
        "platform.roles.assigned",
        "platform.roles.revoked",
        "platform.departments.created",
        "platform.departments.updated",
        "platform.departments.deactivated",
        "platform.departments.reactivated",
        "platform.departments.member_added",
        "platform.departments.member_removed",
        "platform.departments.primary_set",
        "platform.tenants.created",
        "platform.tenants.profile_updated",
        "platform.tenants.contacts_updated",
        "platform.tenants.settings_updated",
        "platform.tenants.account_manager_assigned",
        "platform.tenants.tags_changed",
        "platform.tenants.note_added",
        "platform.tenants.slug_corrected",
        "platform.tenants.activated",
        "platform.tenants.suspended",
        "platform.tenants.reactivated",
        "platform.tenants.deactivated",
        "platform.tenants.banned",
        "platform.registrations.started",
        "platform.registrations.email_verified",
        "platform.registrations.email_corrected",
        "platform.registrations.verification_resent",
        "platform.registrations.cancelled",
        "platform.plans.created",
        "platform.plans.draft_updated",
        "platform.plans.translation_set",
        "platform.plans.catalogue_set",
        "platform.plans.version_published",
        "platform.plans.draft_started",
        "platform.plans.draft_discarded",
        "platform.plans.retired",
        "platform.plans.reactivated",
        "platform.subscriptions.started",
        "platform.subscriptions.changed",
        "platform.subscriptions.change_scheduled",
        "platform.subscriptions.scheduled_change_cancelled",
        "platform.subscriptions.trial_extended",
        "platform.subscriptions.trial_ended",
        "platform.subscriptions.trial_expired",
        "platform.subscriptions.cancellation_requested",
        "platform.subscriptions.resumed",
        "platform.subscriptions.ended",
        "platform.subscriptions.offline_payment_recorded",
        "platform.subscriptions.renewed",
        "platform.subscriptions.suspended_with_tenant",
        "platform.subscriptions.restored_with_tenant",
        "platform.subscriptions.ended_with_tenant",
        "platform.subscriptions.override_set",
        "platform.subscriptions.override_removed",
        "platform.registrations.plan_chosen",
        "platform.plans.version_submitted",
        "platform.plans.version_rejected",
        "platform.discounts.requested",
        "platform.discounts.applied",
        "platform.discounts.approved",
        "platform.discounts.rejected",
        "platform.discounts.ended",
        "platform.subscriptions.charge_added",
        "platform.subscriptions.charge_cancelled",
        "platform.subscriptions.setup_fee_changed",
        "platform.subscriptions.activated_nothing_due",
        "platform.promotions.created",
        "platform.promotions.updated",
        "platform.promotions.name_set",
        "platform.promotions.submitted",
        "platform.promotions.approved",
        "platform.promotions.rejected",
        "platform.promotions.paused",
        "platform.promotions.resumed",
        "platform.promotions.ended",
        "platform.promotions.redeemed",
        "platform.offers.prepared",
        "platform.offers.updated",
        "platform.offers.submitted",
        "platform.offers.issued",
        "platform.offers.approved",
        "platform.offers.rejected",
        "platform.offers.revoked",
        "platform.offers.link_replaced",
        "platform.offers.used",
        "platform.sales_settings.updated"
    };

    private static readonly HashSet<string> Known = new(Catalogue, StringComparer.Ordinal);

    public static bool IsKnown(string? actionCode)
    {
        return actionCode is not null && Known.Contains(actionCode);
    }
}
