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
        "platform.registrations.cancelled"
    };

    private static readonly HashSet<string> Known = new(Catalogue, StringComparer.Ordinal);

    public static bool IsKnown(string? actionCode)
    {
        return actionCode is not null && Known.Contains(actionCode);
    }
}
