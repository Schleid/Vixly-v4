using Vixly.Operations.Web.Features.Help;

namespace Vixly.Operations.Web.Tests;

// =========================================================
// FILE: OperationsHelpTestCatalogue.cs
//
// PURPOSE:
// Supplies the mapped setup route templates to content tests.
//
// RESPONSIBILITIES:
// - Keep test readers independent of a database and HTTP host.
//
// IMPORTANT NOTES:
// - HTTP startup tests separately validate against real endpoints.
// - Add a route here when an article names a new page.
// =========================================================
internal static class OperationsHelpTestCatalogue
{
    public static OperationsHelpCatalogue Create() => new(new HashSet<string>
    {
        "/staff", "/staff/account", "/staff/settings", "/staff/locations", "/staff/services", "/staff/charges",
        "/staff/locations/branch/{branchId:guid?}", "/staff/locations/warehouse/{warehouseId:guid?}",
        "/staff/locations/warehouse/{warehouseId:guid}/storage", "/staff/locations/warehouse/{warehouseId:guid}/printers/",
        "/staff/insurance/", "/staff/inspection/setup", "/staff/settings/invoices", "/staff/settings/payments",
        "/staff/settings/credit", "/staff/settings/bundles", "/staff/settings/events", "/staff/settings/pickup",
        "/staff/settings/delivery", "/staff/supplies", "/staff/supplies/{supplyId:guid}",
        "/staff/carriers", "/staff/carriers/{carrierId:guid}",
        "/staff/billing", "/staff/customers", "/staff/customers/new", "/staff/items", "/staff/settings/items",
        "/staff/site", "/staff/site/testimonials", "/staff/support", "/staff/support/requests", "/staff/tickets/"
    });
}
