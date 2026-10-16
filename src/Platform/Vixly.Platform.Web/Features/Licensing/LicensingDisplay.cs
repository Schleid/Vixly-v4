using Microsoft.Extensions.Localization;
using Vixly.Platform.Application.Queries;
using Vixly.Platform.Domain.Licensing;
using Vixly.Shared.Localization;
using Vixly.Shared.UI.Primitives;

namespace Vixly.Platform.Web.Features.Licensing;

// =========================================================
// FILE: LicensingDisplay.cs
//
// PURPOSE:
// Text keys and badges for the licensing screens.
//
// RESPONSIBILITIES:
// - Map device statuses, kinds, proofs and reasons to text keys
// - Pick a badge per status
// - Write a history line from its code
//
// IMPORTANT NOTES:
// - Display only
// =========================================================
public static class LicensingDisplay
{
    public static string StatusKey(PlatformDeviceStatus status) => "ui.device.status." + Billing.BillingDisplay.Snake(status.ToString());

    public static string KindKey(PlatformDeviceKind kind) => "ui.device.kind." + Billing.BillingDisplay.Snake(kind.ToString());

    public static string ProofKey(PlatformDeviceProof proof) => "ui.device.proof." + Billing.BillingDisplay.Snake(proof.ToString());

    public static string ReasonKey(PlatformDeviceReason reason) => "ui.device.reason." + Billing.BillingDisplay.Snake(reason.ToString());

    public static VxBadgeStatus Badge(PlatformDeviceStatus status, bool seatHeld)
    {
        return status switch
        {
            PlatformDeviceStatus.Active => VxBadgeStatus.Active,
            PlatformDeviceStatus.Pending => VxBadgeStatus.Info,
            PlatformDeviceStatus.ReleasePending => VxBadgeStatus.Warning,
            PlatformDeviceStatus.Revoked => seatHeld ? VxBadgeStatus.Warning : VxBadgeStatus.Danger,
            _ => VxBadgeStatus.Deactivated
        };
    }

    public static bool AwaitsReview(PlatformDeviceItem device)
    {
        ArgumentNullException.ThrowIfNull(device);
        return device.Status == PlatformDeviceStatus.ReleasePending || (device.Status == PlatformDeviceStatus.Revoked && device.SeatHeld);
    }

    // "platform.device_history.approved" -> "ui.device_history.approved".
    public static string History(IStringLocalizer text, string eventCode)
    {
        ArgumentNullException.ThrowIfNull(text);
        return text["ui.device_history." + eventCode[(eventCode.LastIndexOf('.') + 1)..]].Value;
    }
}
