namespace Vixly.Platform.Application.Security;

// =========================================================
// FILE: PlatformPermissions.cs
//
// PURPOSE:
// The catalogue of Platform permission codes.
//
// RESPONSIBILITIES:
// - Declare every permission code in one place
// - List them for validation and for the role editor
//
// IMPORTANT NOTES:
// - The code is the identity of a permission; there is no
//   permission table (Slice 1 design note, section 2)
// - Codes follow the error-code style, plane first
//   (Global Standard 4.2). V3's 69 codes are carried over
//   slice by slice; each slice adds its own here
// - V3 had both X.Read and X.View in every area with no
//   difference in use; V4 keeps one, view
// - Display names and descriptions live in
//   Vixly.Platform.Resources (L-6), keyed by the code
// - Never rename a shipped code: roles store it
// - platform.users.change_authority is never granted through a
//   role: only the SuperUser authority level carries it
// =========================================================
public static class PlatformPermissions
{
    public const string UsersView = "platform.users.view";

    public const string UsersCreate = "platform.users.create";

    public const string UsersEdit = "platform.users.edit";

    public const string UsersInvite = "platform.users.invite";

    public const string UsersSendPasswordReset = "platform.users.send_password_reset";

    public const string UsersLock = "platform.users.lock";

    public const string UsersSuspend = "platform.users.suspend";

    public const string UsersReactivate = "platform.users.reactivate";

    public const string UsersDeactivate = "platform.users.deactivate";

    public const string UsersResetSecondFactor = "platform.users.reset_second_factor";

    public const string UsersEndSessions = "platform.users.end_sessions";

    public const string UsersChangeAuthority = "platform.users.change_authority";

    public const string RolesView = "platform.roles.view";

    public const string RolesCreate = "platform.roles.create";

    public const string RolesEdit = "platform.roles.edit";

    public const string RolesDeactivate = "platform.roles.deactivate";

    public const string RolesEditPermissions = "platform.roles.edit_permissions";

    public const string RolesAssign = "platform.roles.assign";

    public const string DepartmentsView = "platform.departments.view";

    public const string DepartmentsCreate = "platform.departments.create";

    public const string DepartmentsEdit = "platform.departments.edit";

    public const string DepartmentsDeactivate = "platform.departments.deactivate";

    public const string DepartmentsManageMembers = "platform.departments.manage_members";

    public const string AuditView = "platform.audit.view";

    public const string TenantsView = "platform.tenants.view";

    public const string TenantsCreate = "platform.tenants.create";

    public const string TenantsEdit = "platform.tenants.edit";

    public const string TenantsAddNotes = "platform.tenants.add_notes";

    public const string TenantsActivate = "platform.tenants.activate";

    public const string TenantsSuspend = "platform.tenants.suspend";

    public const string TenantsReactivate = "platform.tenants.reactivate";

    public const string TenantsDeactivate = "platform.tenants.deactivate";

    public const string TenantsBan = "platform.tenants.ban";

    public const string TenantsReviewChanges = "platform.tenants.review_changes";

    public const string PlansView = "platform.plans.view";

    public const string PlansManage = "platform.plans.manage";

    public const string PlansPublish = "platform.plans.publish";

    public const string SubscriptionsView = "platform.subscriptions.view";

    public const string SubscriptionsManage = "platform.subscriptions.manage";

    public const string SubscriptionsOverrideLimits = "platform.subscriptions.override_limits";

    public const string DiscountsGrant = "platform.discounts.grant";

    public const string DiscountsApprove = "platform.discounts.approve";

    public const string OffersManage = "platform.offers.manage";

    public const string PromotionsManage = "platform.promotions.manage";

    public const string PromotionsApprove = "platform.promotions.approve";

    public const string SubscriptionsCharges = "platform.subscriptions.charges";

    public const string SalesConfigure = "platform.sales.configure";

    public const string BillingView = "platform.billing.view";

    public const string BillingRecordPayments = "platform.billing.record_payments";

    public const string BillingRequestCredits = "platform.billing.request_credits";

    public const string BillingApproveCredits = "platform.billing.approve_credits";

    public const string BillingConfigure = "platform.billing.configure";

    public const string BillingChargeCards = "platform.billing.charge_cards";

    public const string BillingRefundCards = "platform.billing.refund_cards";

    public const string PaymentsConfigure = "platform.payments.configure";

    public const string CollectionsView = "platform.collections.view";

    public const string CollectionsManage = "platform.collections.manage";

    public const string CollectionsExtend = "platform.collections.extend";

    public const string CollectionsExtendBeyondLimit = "platform.collections.extend_beyond_limit";

    public const string CollectionsConfigure = "platform.collections.configure";

    public const string LicensingView = "platform.licensing.view";

    public const string LicensingManage = "platform.licensing.manage";

    public const string LicensingApproveRelease = "platform.licensing.approve_release";

    public const string LegalView = "platform.legal.view";

    public const string LegalManage = "platform.legal.manage";

    public const string LegalPublish = "platform.legal.publish";

    public const string LegalRemind = "platform.legal.remind";

    public const string LegalRecordAcceptance = "platform.legal.record_acceptance";

    public const string LeadsView = "platform.leads.view";

    public const string LeadsManage = "platform.leads.manage";

    public static IReadOnlyList<string> Catalogue { get; } = new[]
    {
        UsersView,
        UsersCreate,
        UsersEdit,
        UsersInvite,
        UsersSendPasswordReset,
        UsersLock,
        UsersSuspend,
        UsersReactivate,
        UsersDeactivate,
        UsersResetSecondFactor,
        UsersEndSessions,
        UsersChangeAuthority,
        RolesView,
        RolesCreate,
        RolesEdit,
        RolesDeactivate,
        RolesEditPermissions,
        RolesAssign,
        DepartmentsView,
        DepartmentsCreate,
        DepartmentsEdit,
        DepartmentsDeactivate,
        DepartmentsManageMembers,
        AuditView,
        TenantsView,
        TenantsCreate,
        TenantsEdit,
        TenantsAddNotes,
        TenantsActivate,
        TenantsSuspend,
        TenantsReactivate,
        TenantsDeactivate,
        TenantsBan,
        TenantsReviewChanges,
        PlansView,
        PlansManage,
        PlansPublish,
        SubscriptionsView,
        SubscriptionsManage,
        SubscriptionsOverrideLimits,
        DiscountsGrant,
        DiscountsApprove,
        OffersManage,
        PromotionsManage,
        PromotionsApprove,
        SubscriptionsCharges,
        SalesConfigure,
        BillingView,
        BillingRecordPayments,
        BillingRequestCredits,
        BillingApproveCredits,
        BillingConfigure,
        BillingChargeCards,
        BillingRefundCards,
        PaymentsConfigure,
        CollectionsView,
        CollectionsManage,
        CollectionsExtend,
        CollectionsExtendBeyondLimit,
        CollectionsConfigure,
        LicensingView,
        LicensingManage,
        LicensingApproveRelease,
        LegalView,
        LegalManage,
        LegalPublish,
        LegalRemind,
        LegalRecordAcceptance,
        LeadsView,
        LeadsManage
    };

    public static IReadOnlyCollection<string> All { get; } = new HashSet<string>(Catalogue, StringComparer.Ordinal);

    public static IReadOnlyList<string> Grantable { get; } =
        Catalogue.Where(code => code != UsersChangeAuthority).ToArray();

    public static bool IsKnown(string? code)
    {
        return code is not null && All.Contains(code);
    }

    public static bool IsGrantable(string? code)
    {
        return IsKnown(code) && code != UsersChangeAuthority;
    }
}
