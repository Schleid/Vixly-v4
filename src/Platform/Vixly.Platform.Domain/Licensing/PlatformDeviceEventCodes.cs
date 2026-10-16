namespace Vixly.Platform.Domain.Licensing;

// =========================================================
// FILE: PlatformDeviceEventCodes.cs
//
// PURPOSE:
// The history codes of devices and device links (D-L14).
//
// RESPONSIBILITIES:
// - Name every step a device or link can record
//
// IMPORTANT NOTES:
// - Each code has en, fr and es text; never reword one that shipped
// =========================================================
public static class PlatformDeviceEventCodes
{
    public const string Requested = "platform.device_history.requested";
    public const string Approved = "platform.device_history.approved";
    public const string Denied = "platform.device_history.denied";
    public const string Released = "platform.device_history.released";
    public const string ReleaseRequested = "platform.device_history.release_requested";
    public const string ReleaseApproved = "platform.device_history.release_approved";
    public const string ReleaseRefused = "platform.device_history.release_refused";
    public const string ReleaseWithdrawn = "platform.device_history.release_withdrawn";
    public const string Revoked = "platform.device_history.revoked";
    public const string Expired = "platform.device_history.expired";
    public const string EnrolledByLink = "platform.device_history.enrolled_by_link";
    public const string LinkSent = "platform.device_history.link_sent";

    public static IReadOnlyList<string> All { get; } = new[]
    {
        Requested, Approved, Denied, Released, ReleaseRequested, ReleaseApproved, ReleaseRefused, ReleaseWithdrawn, Revoked, Expired, EnrolledByLink, LinkSent
    };
}
