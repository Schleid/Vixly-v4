namespace Vixly.Platform.Application.Collections;

// =========================================================
// FILE: PlatformCollectionsNoticeKind.cs
//
// PURPOSE:
// The emails collections sends to a tenant (D-C11).
//
// RESPONSIBILITIES:
// - Name each email
//
// IMPORTANT NOTES:
// - Part of message payloads; never renumber
// =========================================================
public enum PlatformCollectionsNoticeKind
{
    Reminder1 = 1,
    Reminder2 = 2,
    FinalWarning = 3,
    Blocked = 4,
    ExtensionGranted = 5,
    ExtensionEnding = 6,
    AccessRestored = 7
}
