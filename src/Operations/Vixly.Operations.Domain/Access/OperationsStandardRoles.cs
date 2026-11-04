namespace Vixly.Operations.Domain.Access;

// =========================================================
// FILE: OperationsStandardRoles.cs
//
// PURPOSE:
// The ready-made roles every company starts with (owner, 2026-10-29: branches and roles design, section 5, R-5).
//
// RESPONSIBILITIES:
// - Name each standard role by a stable key and list its permissions
//
// IMPORTANT NOTES:
// - The company can rename, change, deactivate or keep them, and add its own; Vixly never changes them after creating them
// - The key keeps a renamed role from being created again
// - Branch and warehouse roles find one customer at a time (ops.customers.lookup); the customer list is for company roles (design 4.4)
// =========================================================
public static class OperationsStandardRoles
{
    public const int KeyMaxLength = 40;

    public static IReadOnlyList<(string Key, IReadOnlyList<string> Permissions)> All { get; } = new (string, IReadOnlyList<string>)[]
    {
        ("security_admin", new[]
        {
            OperationsPermissions.StaffView, OperationsPermissions.StaffInvite, OperationsPermissions.StaffEdit, OperationsPermissions.StaffLock,
            OperationsPermissions.StaffSuspend, OperationsPermissions.StaffReactivate, OperationsPermissions.StaffDeactivate,
            OperationsPermissions.StaffResetSecondFactor, OperationsPermissions.StaffEndSessions,
            OperationsPermissions.RolesView, OperationsPermissions.RolesCreate, OperationsPermissions.RolesEdit, OperationsPermissions.RolesEditPermissions,
            OperationsPermissions.RolesGrant,
            OperationsPermissions.DevicesView, OperationsPermissions.DevicesApprove, OperationsPermissions.DevicesRelease,
            OperationsPermissions.AuditView, OperationsPermissions.SettingsView
        }),
        ("finance", new[]
        {
            OperationsPermissions.CustomersView, OperationsPermissions.CustomersCredit, OperationsPermissions.InvoicesView,
            OperationsPermissions.PaymentsTake, OperationsPermissions.PaymentsConfirm, OperationsPermissions.PaymentsRefund,
            OperationsPermissions.CreditsRequest, OperationsPermissions.CreditsApprove,
            OperationsPermissions.BillsEnter, OperationsPermissions.BillsApprove, OperationsPermissions.CostsView,
            OperationsPermissions.ReportsFinance, OperationsPermissions.ReportsOperations
        }),
        ("customer_service", new[]
        {
            OperationsPermissions.CustomersView, OperationsPermissions.CustomersManage, OperationsPermissions.QuotesCreate,
            OperationsPermissions.ShipmentsView, OperationsPermissions.ShipmentsManage, OperationsPermissions.InsuranceView, OperationsPermissions.InsuranceManage,
            OperationsPermissions.EventsView, OperationsPermissions.EventsAdd, OperationsPermissions.HoldsManage
        }),
        ("branch_manager", Manager(withDiscounts: true)),
        ("warehouse_manager", Manager(withDiscounts: false)),
        ("counter_clerk", new[]
        {
            OperationsPermissions.CounterServe, OperationsPermissions.CustomersLookup, OperationsPermissions.ShipmentsView, OperationsPermissions.ShipmentsManage,
            OperationsPermissions.ParcelsView, OperationsPermissions.QuotesCreate, OperationsPermissions.SuppliesStock, OperationsPermissions.InvoicesView
        }),
        ("cashier", new[]
        {
            OperationsPermissions.CustomersLookup, OperationsPermissions.InvoicesView, OperationsPermissions.PaymentsTake, OperationsPermissions.ShipmentsView,
            OperationsPermissions.CreditsRequest
        }),
        ("warehouse_worker", new[]
        {
            OperationsPermissions.ReceivingReceive, OperationsPermissions.ParcelsView, OperationsPermissions.ParcelsAssign,
            OperationsPermissions.InspectionCheck, OperationsPermissions.InspectionPerform, OperationsPermissions.BundlesFill,
            OperationsPermissions.ArrivalsReceive, OperationsPermissions.EventsAdd, OperationsPermissions.PickupRelease
        }),
        ("driver", new[] { OperationsPermissions.DeliveryDrive, OperationsPermissions.PickupRelease, OperationsPermissions.EventsAdd }),
        ("support_agent", new[] { OperationsPermissions.SupportLookup })
    };

    private static IReadOnlyList<string> Manager(bool withDiscounts)
    {
        List<string> permissions = new()
        {
            OperationsPermissions.ShipmentsView, OperationsPermissions.ShipmentsManage, OperationsPermissions.ParcelsView, OperationsPermissions.ParcelsAssign,
            OperationsPermissions.ReceivingReceive, OperationsPermissions.InspectionCheck, OperationsPermissions.InspectionPerform, OperationsPermissions.HoldsManage,
            OperationsPermissions.BundlesManage, OperationsPermissions.BundlesFill, OperationsPermissions.DispatchesManage, OperationsPermissions.ArrivalsReceive,
            OperationsPermissions.CustomsRecord, OperationsPermissions.PickupRelease, OperationsPermissions.DeliveryPlan,
            OperationsPermissions.EventsView, OperationsPermissions.EventsAdd, OperationsPermissions.QuotesCreate, OperationsPermissions.CounterServe,
            OperationsPermissions.SuppliesStock, OperationsPermissions.CreditsRequest, OperationsPermissions.InvoicesView, OperationsPermissions.PaymentsTake,
            OperationsPermissions.ReportsOperations, OperationsPermissions.CustomersLookup, OperationsPermissions.BranchesView, OperationsPermissions.WarehousesView,
            OperationsPermissions.PrintersManage
        };

        if (withDiscounts)
        {
            permissions.Add(OperationsPermissions.DiscountsApprove);
        }

        return permissions;
    }
}
