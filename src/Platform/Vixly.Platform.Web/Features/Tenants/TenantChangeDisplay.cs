using Vixly.Platform.Domain.Tenants;
using Vixly.Shared.UI.Primitives;

namespace Vixly.Platform.Web.Features.Tenants;

// =========================================================
// FILE: TenantChangeDisplay.cs
//
// PURPOSE:
// Text keys and badges for company change requests (B12).
//
// RESPONSIBILITIES:
// - Map each status to its text key and badge
//
// IMPORTANT NOTES:
// - Text lives in PlatformText (UI-10)
// =========================================================
public static class TenantChangeDisplay
{
    public static string StatusKey(PlatformTenantChangeStatus status)
    {
        return "ui.tenant_changes.status." + status.ToString().ToLowerInvariant();
    }

    public static VxBadgeStatus Badge(PlatformTenantChangeStatus status)
    {
        return status switch
        {
            PlatformTenantChangeStatus.Pending => VxBadgeStatus.Warning,
            PlatformTenantChangeStatus.Approved => VxBadgeStatus.Success,
            PlatformTenantChangeStatus.Rejected => VxBadgeStatus.Danger,
            _ => VxBadgeStatus.Info
        };
    }
}
