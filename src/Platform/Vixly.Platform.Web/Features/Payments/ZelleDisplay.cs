using Vixly.Platform.Domain.Payments;
using Vixly.Shared.UI.Primitives;

namespace Vixly.Platform.Web.Features.Payments;

// =========================================================
// FILE: ZelleDisplay.cs
//
// PURPOSE:
// Text keys and badges for the Zelle screens.
//
// RESPONSIBILITIES:
// - Map report statuses and sources to text keys and badges
//
// IMPORTANT NOTES:
// - Display only
// =========================================================
public static class ZelleDisplay
{
    public static string StatusKey(PlatformZelleClaimStatus status) => "ui.zelle.status." + Billing.BillingDisplay.Snake(status.ToString());

    public static string SourceKey(PlatformZelleClaimSource source) => "ui.zelle.source." + Billing.BillingDisplay.Snake(source.ToString());

    public static VxBadgeStatus Badge(PlatformZelleClaimStatus status)
    {
        return status switch
        {
            PlatformZelleClaimStatus.Waiting => VxBadgeStatus.Warning,
            PlatformZelleClaimStatus.Confirmed => VxBadgeStatus.Success,
            _ => VxBadgeStatus.Deactivated
        };
    }
}
