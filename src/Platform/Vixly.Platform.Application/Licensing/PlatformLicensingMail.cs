namespace Vixly.Platform.Application.Licensing;

// =========================================================
// FILE: PlatformLicensingMail.cs
//
// PURPOSE:
// What one licensing email says (D-L16).
//
// RESPONSIBILITIES:
// - Carry the kind, the link and the device facts the mailer needs
//
// IMPORTANT NOTES:
// - The link carries its token after "#" so it never reaches server logs
// =========================================================
public sealed record PlatformLicensingMail(
    PlatformLicensingNoticeKind Kind,
    string? Link,
    DateTimeOffset? LinkExpiresUtc,
    string? DeviceNumber,
    string? DeviceName,
    bool? Approved,
    int Count);
