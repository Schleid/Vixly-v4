using Vixly.Operations.Domain.Access;

namespace Vixly.Operations.Application.Dashboard;

// =========================================================
// FILE: OperationsDashboardCounters.cs
//
// PURPOSE:
// The counters the staff home page can show (Slice 11).
//
// RESPONSIBILITIES:
// - Name each counter, the right that shows it, the list it opens and whether it is counted per warehouse
//
// IMPORTANT NOTES:
// - In menu order; a counter is only ever added
// =========================================================
public static class OperationsDashboardCounters
{
    public static IReadOnlyList<(string Key, string Permission, string Link, bool PerWarehouse)> All { get; } = new[]
    {
        ("to_inspect", OperationsPermissions.InspectionPerform, "/staff/inspection/queue", true),
        ("holds", OperationsPermissions.HoldsManage, "/staff/holds", true),
        // Owner, 2026-10-29 (ticket desk): one counter per department: the warehouse's package tickets, Finance's billing tickets, customer service's others.
        ("tickets", OperationsPermissions.ParcelsView, "/staff/tickets?topic=Package", true),
        ("billing_tickets", OperationsPermissions.InvoicesView, "/staff/tickets?topic=Billing", true),
        ("service_tickets", OperationsPermissions.CustomersManage, "/staff/tickets", false),
        ("open_shipments", OperationsPermissions.ShipmentsView, "/staff/shipments", true),
        ("to_dispatch", OperationsPermissions.DispatchesManage, "/staff/dispatches", true),
        ("arriving", OperationsPermissions.ArrivalsReceive, "/staff/arrivals", true),
        ("ready_for_pickup", OperationsPermissions.PickupRelease, "/staff/pickup", true),
        ("payments_to_verify", OperationsPermissions.PaymentsConfirm, "/staff/cashier/verify", true),
        ("invoices_overdue", OperationsPermissions.InvoicesView, "/staff/invoices", true),
        ("credit_charges", OperationsPermissions.CustomersCredit, "/staff/credit/charges", false),
        ("bills_to_approve", OperationsPermissions.BillsApprove, "/staff/bills", false),
        ("bills_overdue", OperationsPermissions.BillsApprove, "/staff/bills", false),
        ("webhooks_off", OperationsPermissions.SettingsEdit, "/staff/settings/api#webhooks", false)
    };
}
