namespace Vixly.Platform.Application.Security;

// =========================================================
// FILE: PlatformSystemRoleSyncResult.cs
//
// PURPOSE:
// What one run of the system role synchronisation did.
//
// RESPONSIBILITIES:
// - List the roles created, corrected and left alone because of a
//   name conflict; say whether the changes were saved
//
// IMPORTANT NOTES:
// - Logged by the host at start; never shown to users
// =========================================================
public sealed record PlatformSystemRoleSyncResult(
    IReadOnlyList<string> Created,
    IReadOnlyList<string> Corrected,
    IReadOnlyList<string> Conflicts,
    bool Saved);
