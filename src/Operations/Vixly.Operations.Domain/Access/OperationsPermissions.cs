namespace Vixly.Operations.Domain.Access;

// =========================================================
// FILE: OperationsPermissions.cs
//
// PURPOSE:
// The catalogue of staff permissions (Slice 1D, design section 4).
//
// RESPONSIBILITIES:
// - Name every permission code
// - Say which ones only the system roles may hold
//
// IMPORTANT NOTES:
// - Codes are ops.<area>.<action>; later slices add theirs
// - Company contacts and change requests, and device approval and release, are reserved to the Owner and Administrators: Platform accepts them only from owner or admin (design section 3)
// =========================================================
public static class OperationsPermissions
{
    public const string StaffView = "ops.staff.view";
    public const string StaffInvite = "ops.staff.invite";
    public const string StaffEdit = "ops.staff.edit";
    public const string StaffLock = "ops.staff.lock";
    public const string StaffSuspend = "ops.staff.suspend";
    public const string StaffReactivate = "ops.staff.reactivate";
    public const string StaffDeactivate = "ops.staff.deactivate";
    public const string StaffResetSecondFactor = "ops.staff.reset_second_factor";
    public const string StaffEndSessions = "ops.staff.end_sessions";
    public const string RolesView = "ops.roles.view";
    public const string RolesCreate = "ops.roles.create";
    public const string RolesEdit = "ops.roles.edit";
    public const string RolesEditPermissions = "ops.roles.edit_permissions";
    public const string RolesGrant = "ops.roles.grant";
    public const string DevicesView = "ops.devices.view";
    public const string DevicesApprove = "ops.devices.approve";
    public const string DevicesRelease = "ops.devices.release";
    public const string SettingsView = "ops.settings.view";
    public const string SettingsEdit = "ops.settings.edit";
    public const string CompanyView = "ops.company.view";
    public const string CompanyEditContacts = "ops.company.edit_contacts";
    public const string CompanyRequestChange = "ops.company.request_change";
    public const string BranchesView = "ops.branches.view";
    public const string BranchesManage = "ops.branches.manage";
    public const string WarehousesView = "ops.warehouses.view";
    public const string WarehousesManage = "ops.warehouses.manage";
    public const string ServicesView = "ops.services.view";
    public const string ServicesManage = "ops.services.manage";
    public const string BrandView = "ops.brand.view";
    public const string BrandEdit = "ops.brand.edit";
    public const string BrandPublish = "ops.brand.publish";
    public const string AuditView = "ops.audit.view";
    public const string CustomersView = "ops.customers.view";
    public const string CustomersManage = "ops.customers.manage";
    // Slice 5F step 2b (K-1): the credit department's right: open and manage credit accounts, decide charges above a limit.
    public const string CustomersCredit = "ops.customers.credit";

    // Slice 3B: given per warehouse (a warehouse grant counts for that warehouse's parcels).
    public const string ReceivingReceive = "ops.receiving.receive";

    public const string ParcelsView = "ops.parcels.view";

    public const string ParcelsAssign = "ops.parcels.assign";

    // The inspection station: condition, weight, size and photos (Slice 3B, owner 2026-10-18).
    public const string InspectionCheck = "ops.inspection.check";

    // Label printers and the dock print program of a warehouse.
    public const string PrintersManage = "ops.printers.manage";

    // Slice 3C: the company's restricted-items list, inspection rules and random share (company-wide).
    public const string InspectionConfigure = "ops.inspection.configure";

    // Slice 3C: the inspection queue and outcomes, given per warehouse.
    public const string InspectionPerform = "ops.inspection.perform";

    // Slice 3C: the holds page; releasing a hold with a note, given per warehouse.
    public const string HoldsManage = "ops.holds.manage";

    // Slice 4: rate cards, extras and the insurance plan (company-wide).
    public const string ChargesConfigure = "ops.charges.configure";

    // Slice 4: quotes for a customer, with a preferred price (company-wide).
    public const string QuotesCreate = "ops.quotes.create";

    // Slice 5A: the shipments desk and the shipment documents; changing the service before departure. Given per warehouse (origin).
    public const string ShipmentsView = "ops.shipments.view";

    public const string ShipmentsManage = "ops.shipments.manage";

    // Slice 5B: the company's policies, the cover it sells and its liability terms (company-wide).
    public const string InsuranceView = "ops.insurance.view";

    public const string InsuranceManage = "ops.insurance.manage";

    // Slice 5C: serve customers at the front-desk counter: find them, create their shipments, enter their parcels. Given per warehouse.
    public const string CounterServe = "ops.counter.serve";

    // Slice 5E: receive, adjust, transfer and count the supplies stock. Given per warehouse.
    public const string SuppliesStock = "ops.supplies.stock";

    // Slice 5E (S-18): approve a supplies discount above a clerk's limit. Given per warehouse.
    public const string DiscountsApprove = "ops.discounts.approve";

    // Slice 5F (I-9): see invoices, ask for a credit note or a void, approve one. Given per warehouse (the invoice's).
    public const string InvoicesView = "ops.invoices.view";

    public const string CreditsRequest = "ops.credits.request";

    public const string CreditsApprove = "ops.credits.approve";

    // Slice 5F step 2: take payments at the cashier, confirm Zelle and transfers, give money back. Given per warehouse.
    public const string PaymentsTake = "ops.payments.take";

    public const string PaymentsConfirm = "ops.payments.confirm";

    public const string PaymentsRefund = "ops.payments.refund";

    // Slice 6A (D-11): open, close and void bundles, switch a consolidated shipment's way (the ship manager); scan parcels into bundles and repack boxes (workers). Given per warehouse.
    public const string BundlesManage = "ops.bundles.manage";

    public const string BundlesFill = "ops.bundles.fill";

    // Slice 6B (D-8): dispatches (per warehouse) and the company's carriers and their rates.
    public const string DispatchesManage = "ops.dispatches.manage";

    public const string CarriersManage = "ops.carriers.manage";

    // Slice 6C (E-6): the events workspace (per warehouse) and recording events by hand.
    public const string EventsView = "ops.events.view";

    public const string EventsAdd = "ops.events.add";

    // Slice 7 (R-1 to R-3): receiving at a destination warehouse, and recording customs there; given per warehouse.
    public const string ArrivalsReceive = "ops.arrivals.receive";

    public const string CustomsRecord = "ops.customs.record";

    // Slice 7 step 2 (R-5, R-9): the pickup counter at a destination: charges, proof, release; given per warehouse.
    public const string PickupRelease = "ops.pickup.release";

    public static IReadOnlyList<string> All { get; } = new[]
    {
        StaffView, StaffInvite, StaffEdit, StaffLock, StaffSuspend, StaffReactivate, StaffDeactivate, StaffResetSecondFactor, StaffEndSessions,
        RolesView, RolesCreate, RolesEdit, RolesEditPermissions, RolesGrant,
        DevicesView, DevicesApprove, DevicesRelease,
        SettingsView, SettingsEdit,
        CompanyView, CompanyEditContacts, CompanyRequestChange,
        BranchesView, BranchesManage, WarehousesView, WarehousesManage,
        ServicesView, ServicesManage,
        BrandView, BrandEdit, BrandPublish,
        AuditView,
        CustomersView, CustomersManage, CustomersCredit,
        ReceivingReceive, ParcelsView, ParcelsAssign, InspectionCheck, PrintersManage,
        InspectionConfigure, InspectionPerform, HoldsManage,
        ChargesConfigure, QuotesCreate,
        ShipmentsView, ShipmentsManage,
        InsuranceView, InsuranceManage,
        CounterServe,
        SuppliesStock,
        DiscountsApprove,
        InvoicesView, CreditsRequest, CreditsApprove,
        PaymentsTake, PaymentsConfirm, PaymentsRefund,
        BundlesManage, BundlesFill,
        DispatchesManage, CarriersManage,
        EventsView, EventsAdd,
        ArrivalsReceive, CustomsRecord,
        PickupRelease
    };

    // Held only by the system roles; a tenant role can never include them.
    public static IReadOnlyList<string> SystemOnly { get; } = new[] { CompanyEditContacts, CompanyRequestChange, DevicesApprove, DevicesRelease };

    // The Administrator holds everything except the company change request (Owner only).
    public static IReadOnlyList<string> Administrator { get; } = All.Where(code => code != CompanyRequestChange).ToList();

    public static bool IsKnown(string? code) => code is not null && All.Contains(code, StringComparer.Ordinal);
}
