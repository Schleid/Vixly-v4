namespace Vixly.Platform.Application.Security;

// =========================================================
// FILE: PlatformSystemRoles.cs
//
// PURPOSE:
// The protected system roles and the permissions code gives
// them.
//
// RESPONSIBILITIES:
// - Name each system role by its stable key
// - Define each role's permissions in code, so deploys keep
//   the database in step (SynchronizeSystemPermissions)
//
// IMPORTANT NOTES:
// - SuperUser is an authority level, not a role: a SuperUser
//   holds every permission
// - security_administrator owns identity, sessions and audit
// - administrator owns business administration and may view
//   staff, but never changes identity or security
// - Later slices extend administrator with their business
//   permissions
// =========================================================
public static class PlatformSystemRoles
{
    public const string SecurityAdministrator = "security_administrator";

    public const string Administrator = "administrator";

    public const string Salesperson = "salesperson";

    public const string SalesManager = "sales_manager";

    public const string ChiefFinancialOfficer = "chief_financial_officer";

    public const string BillingClerk = "billing_clerk";

    public static IReadOnlyDictionary<string, IReadOnlyCollection<string>> Definitions { get; } =
        new Dictionary<string, IReadOnlyCollection<string>>(StringComparer.Ordinal)
        {
            [SecurityAdministrator] = new[]
            {
                PlatformPermissions.UsersView,
                PlatformPermissions.UsersCreate,
                PlatformPermissions.UsersEdit,
                PlatformPermissions.UsersInvite,
                PlatformPermissions.UsersSendPasswordReset,
                PlatformPermissions.UsersLock,
                PlatformPermissions.UsersSuspend,
                PlatformPermissions.UsersReactivate,
                PlatformPermissions.UsersDeactivate,
                PlatformPermissions.UsersResetSecondFactor,
                PlatformPermissions.UsersEndSessions,
                PlatformPermissions.RolesView,
                PlatformPermissions.RolesCreate,
                PlatformPermissions.RolesEdit,
                PlatformPermissions.RolesDeactivate,
                PlatformPermissions.RolesEditPermissions,
                PlatformPermissions.RolesAssign,
                PlatformPermissions.DepartmentsView,
                PlatformPermissions.DepartmentsCreate,
                PlatformPermissions.DepartmentsEdit,
                PlatformPermissions.DepartmentsDeactivate,
                PlatformPermissions.DepartmentsManageMembers,
                PlatformPermissions.AuditView,
                PlatformPermissions.LicensingView,
                PlatformPermissions.LicensingManage,
                PlatformPermissions.LegalView
            },
            [Administrator] = new[]
            {
                PlatformPermissions.UsersView,
                PlatformPermissions.RolesView,
                PlatformPermissions.DepartmentsView,
                PlatformPermissions.DepartmentsManageMembers,
                PlatformPermissions.TenantsView,
                PlatformPermissions.TenantsCreate,
                PlatformPermissions.TenantsEdit,
                PlatformPermissions.TenantsAddNotes,
                PlatformPermissions.TenantsActivate,
                PlatformPermissions.TenantsSuspend,
                PlatformPermissions.TenantsReactivate,
                PlatformPermissions.TenantsDeactivate,
                PlatformPermissions.TenantsBan,
                PlatformPermissions.PlansView,
                PlatformPermissions.PlansManage,
                PlatformPermissions.PlansPublish,
                PlatformPermissions.SubscriptionsView,
                PlatformPermissions.SubscriptionsManage,
                PlatformPermissions.SubscriptionsOverrideLimits,
                PlatformPermissions.DiscountsGrant,
                PlatformPermissions.DiscountsApprove,
                PlatformPermissions.OffersManage,
                PlatformPermissions.PromotionsManage,
                PlatformPermissions.PromotionsApprove,
                PlatformPermissions.SubscriptionsCharges,
                PlatformPermissions.SalesConfigure,
                PlatformPermissions.BillingView,
                PlatformPermissions.BillingRecordPayments,
                PlatformPermissions.BillingRequestCredits,
                PlatformPermissions.BillingApproveCredits,
                PlatformPermissions.BillingConfigure,
                PlatformPermissions.BillingChargeCards,
                PlatformPermissions.BillingRefundCards,
                PlatformPermissions.PaymentsConfigure,
                PlatformPermissions.CollectionsView,
                PlatformPermissions.CollectionsManage,
                PlatformPermissions.CollectionsExtend,
                PlatformPermissions.CollectionsExtendBeyondLimit,
                PlatformPermissions.CollectionsConfigure,
                PlatformPermissions.LicensingView,
                PlatformPermissions.LicensingManage,
                PlatformPermissions.LicensingApproveRelease,
                PlatformPermissions.LegalView,
                PlatformPermissions.LegalManage,
                PlatformPermissions.LegalPublish,
                PlatformPermissions.LegalRemind,
                PlatformPermissions.LegalRecordAcceptance
            },
            [Salesperson] = new[]
            {
                PlatformPermissions.TenantsView,
                PlatformPermissions.PlansView,
                PlatformPermissions.PlansManage,
                PlatformPermissions.SubscriptionsView,
                PlatformPermissions.DiscountsGrant,
                PlatformPermissions.OffersManage,
                PlatformPermissions.PromotionsManage,
                PlatformPermissions.SubscriptionsCharges,
                PlatformPermissions.LegalView,
                PlatformPermissions.LegalRemind
            },
            [SalesManager] = new[]
            {
                PlatformPermissions.TenantsView,
                PlatformPermissions.PlansView,
                PlatformPermissions.PlansManage,
                PlatformPermissions.SubscriptionsView,
                PlatformPermissions.SubscriptionsManage,
                PlatformPermissions.DiscountsGrant,
                PlatformPermissions.DiscountsApprove,
                PlatformPermissions.OffersManage,
                PlatformPermissions.PromotionsManage,
                PlatformPermissions.SubscriptionsCharges,
                PlatformPermissions.SalesConfigure,
                PlatformPermissions.BillingView,
                PlatformPermissions.BillingApproveCredits,
                PlatformPermissions.CollectionsView,
                PlatformPermissions.CollectionsExtendBeyondLimit,
                PlatformPermissions.LegalView,
                PlatformPermissions.LegalRemind
            },
            [ChiefFinancialOfficer] = new[]
            {
                PlatformPermissions.TenantsView,
                PlatformPermissions.PlansView,
                PlatformPermissions.PlansPublish,
                PlatformPermissions.SubscriptionsView,
                PlatformPermissions.PromotionsApprove,
                PlatformPermissions.BillingView,
                PlatformPermissions.BillingConfigure,
                PlatformPermissions.BillingRefundCards,
                PlatformPermissions.PaymentsConfigure,
                PlatformPermissions.CollectionsView,
                PlatformPermissions.CollectionsConfigure,
                PlatformPermissions.LegalView
            },
            [BillingClerk] = new[]
            {
                PlatformPermissions.TenantsView,
                PlatformPermissions.TenantsEdit,
                PlatformPermissions.PlansView,
                PlatformPermissions.SubscriptionsView,
                PlatformPermissions.BillingView,
                PlatformPermissions.BillingRecordPayments,
                PlatformPermissions.BillingRequestCredits,
                PlatformPermissions.BillingChargeCards,
                PlatformPermissions.CollectionsView,
                PlatformPermissions.CollectionsManage,
                PlatformPermissions.CollectionsExtend,
                PlatformPermissions.LicensingView,
                PlatformPermissions.LegalView
            }
        };
}
