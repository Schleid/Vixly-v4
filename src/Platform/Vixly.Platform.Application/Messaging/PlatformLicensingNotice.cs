using Vixly.Platform.Application.Licensing;

namespace Vixly.Platform.Application.Messaging;

// =========================================================
// FILE: PlatformLicensingNotice.cs
//
// PURPOSE:
// Outbox message: a licensing email is due (D-L16).
//
// RESPONSIBILITIES:
// - Name the tenant, the kind and the device or registration
//
// IMPORTANT NOTES:
// - Links are created by the handler when it sends, so no token is ever stored in the outbox
// =========================================================
public sealed record PlatformLicensingNotice(
    Guid PlatformTenantId,
    PlatformLicensingNoticeKind Kind,
    Guid? DeviceId,
    Guid? RegistrationId,
    bool? Approved,
    int Count,
    DateTimeOffset OccurredUtc);
