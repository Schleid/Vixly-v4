namespace Vixly.Platform.Application.Maintenance;

// =========================================================
// FILE: PlatformEdgeSendResult.cs
//
// PURPOSE:
// What became of a list sent to the Cloudflare Worker.
//
// RESPONSIBILITIES:
// - Sent: the Worker keeps it
// - Stale: the Worker already has a newer list (nothing to do)
// - NotConfigured: no Worker set up for this installation (staging, a developer's machine)
//
// IMPORTANT NOTES:
// - Only Sent counts as handled on the Jobs page
// =========================================================
public enum PlatformEdgeSendResult
{
    Sent = 0,
    Stale = 1,
    NotConfigured = 2
}
