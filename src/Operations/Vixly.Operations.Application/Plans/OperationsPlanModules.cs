using Vixly.Operations.Application.Ports;
using Vixly.Operations.Domain.Access;

namespace Vixly.Operations.Application.Plans;

// =========================================================
// FILE: OperationsPlanModules.cs
//
// PURPOSE:
// The Operations modules a plan may leave out (Vixly Lite, owner 2026-10-30), with what each one withholds.
//
// RESPONSIBILITIES:
// - Name the modules by their Platform feature code
// - Give the permissions and the staff pages a module withholds when the plan leaves it out
//
// IMPORTANT NOTES:
// - Every plan before Lite includes every module (Platform migration PlanModules); a company without a subscription keeps them all
// - Withheld permissions are refused everywhere, the Owner included; pages are hidden from the menu and answer "not found"
// - Data is never deleted: a company that changes plan finds everything again
// =========================================================
public static class OperationsPlanModules
{
    public const string CustomerPortal = "customer_portal";

    public const string Insurance = "insurance";

    public const string CreditAccounts = "credit_accounts";

    public const string Supplies = "supplies";

    public const string HomeDelivery = "home_delivery";

    public const string SupportDesk = "support_desk";

    public const string Payables = "payables";

    public static IReadOnlyList<string> All { get; } = new[] { CustomerPortal, Insurance, CreditAccounts, Supplies, HomeDelivery, SupportDesk, Payables };

    private static readonly Dictionary<string, string[]> Permissions = new(StringComparer.Ordinal)
    {
        [Insurance] = new[] { OperationsPermissions.InsuranceView, OperationsPermissions.InsuranceManage },
        [CreditAccounts] = new[] { OperationsPermissions.CustomersCredit },
        [Supplies] = new[] { OperationsPermissions.SuppliesStock },
        [HomeDelivery] = new[] { OperationsPermissions.DeliveryPlan, OperationsPermissions.DeliveryDrive },
        [SupportDesk] = new[] { OperationsPermissions.SupportLookup },
        [Payables] = new[] { OperationsPermissions.CostsView, OperationsPermissions.BillsEnter, OperationsPermissions.BillsApprove }
    };

    // Staff pages of each module, also those shared with other permissions (settings, configuration).
    private static readonly Dictionary<string, string[]> Pages = new(StringComparer.Ordinal)
    {
        [Insurance] = new[] { "/staff/insurance" },
        [CreditAccounts] = new[] { "/staff/credit", "/staff/settings/credit" },
        [Supplies] = new[] { "/staff/supplies" },
        [HomeDelivery] = new[] { "/staff/delivery", "/staff/drive", "/staff/settings/delivery" },
        [SupportDesk] = new[] { "/staff/support", "/staff/tickets" },
        [Payables] = new[] { "/staff/bills", "/staff/profit" }
    };

    public static IReadOnlySet<string> WithheldPermissions(OperationsPlanFeatures features)
    {
        ArgumentNullException.ThrowIfNull(features);
        return features.ModulesOff.Where(Permissions.ContainsKey).SelectMany(module => Permissions[module]).ToHashSet(StringComparer.Ordinal);
    }

    public static IReadOnlyList<string> HiddenPages(OperationsPlanFeatures features)
    {
        ArgumentNullException.ThrowIfNull(features);
        return features.ModulesOff.Where(Pages.ContainsKey).SelectMany(module => Pages[module]).ToArray();
    }

    // A page and everything under it ("/staff/credit" hides "/staff/credit/123", never "/staff/credits").
    public static bool Hides(IEnumerable<string> pages, string path)
    {
        ArgumentNullException.ThrowIfNull(pages);
        return path is not null && pages.Any(page => path.Equals(page, StringComparison.OrdinalIgnoreCase) || path.StartsWith(page + "/", StringComparison.OrdinalIgnoreCase));
    }
}
