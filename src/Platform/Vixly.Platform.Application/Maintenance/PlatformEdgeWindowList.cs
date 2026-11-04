namespace Vixly.Platform.Application.Maintenance;

// =========================================================
// FILE: PlatformEdgeWindowList.cs
//
// PURPOSE:
// The whole list of open maintenance windows the Cloudflare Worker keeps.
//
// RESPONSIBILITIES:
// - Carry every window not cancelled and not finished
//
// IMPORTANT NOTES:
// - The Worker replaces its copy with each list; sending twice changes nothing
// - Scheduled windows are included, so the Worker closes the sites on time even if Control is down then
// =========================================================
public sealed record PlatformEdgeWindowList(IReadOnlyList<PlatformEdgeWindow> Windows);
