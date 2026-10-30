using Vixly.Platform.Domain.Sales;
using Vixly.Shared.UI.Primitives;

namespace Vixly.Platform.Web.Features.Sales;

// =========================================================
// FILE: LeadDisplay.cs
//
// PURPOSE:
// Text keys and badges for contact requests in the console.
//
// RESPONSIBILITIES:
// - Map lead states and kinds to text keys and badges
//
// IMPORTANT NOTES:
// - Keys live in PlatformText (UI-10)
// =========================================================
public static class LeadDisplay
{
    public static string StatusKey(PlatformSalesLeadStatus status) => "ui.leads.status." + Billing.BillingDisplay.Snake(status.ToString());

    public static string KindKey(PlatformSalesLeadKind kind) => "ui.leads.kind." + Billing.BillingDisplay.Snake(kind.ToString());

    public static VxBadgeStatus Badge(PlatformSalesLeadStatus status)
    {
        return status switch
        {
            PlatformSalesLeadStatus.New => VxBadgeStatus.Warning,
            PlatformSalesLeadStatus.Contacted => VxBadgeStatus.Success,
            _ => VxBadgeStatus.Deactivated
        };
    }
}
