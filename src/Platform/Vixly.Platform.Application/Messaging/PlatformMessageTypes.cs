namespace Vixly.Platform.Application.Messaging;

// =========================================================
// FILE: PlatformMessageTypes.cs
//
// PURPOSE:
// The versioned types of outbox messages (D-T17).
//
// RESPONSIBILITIES:
// - Name every message the Platform writes to its outbox
//
// IMPORTANT NOTES:
// - A type never changes meaning; a new shape is a new version
// =========================================================
public static class PlatformMessageTypes
{
    public const string TenantStatusChanged = "platform.tenant.status_changed.v1";

    public const string TenantSlugCorrected = "platform.tenant.slug_corrected.v1";
}
