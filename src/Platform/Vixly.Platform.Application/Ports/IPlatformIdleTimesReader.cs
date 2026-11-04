using Vixly.Platform.Application.Identity;

namespace Vixly.Platform.Application.Ports;

// =========================================================
// FILE: IPlatformIdleTimesReader.cs
//
// PURPOSE:
// Reads Control's inactivity times for every signed-in request (owner, 2026-10-27).
//
// RESPONSIBILITIES:
// - Give the current times, kept a short while
// - Forget them when the security administrator changes them
//
// IMPORTANT NOTES:
// - Kept at most one minute: a change applies within the minute on every server
// =========================================================
public interface IPlatformIdleTimesReader
{
    Task<PlatformIdleTimes> GetAsync(CancellationToken cancellationToken);

    void Forget();
}
