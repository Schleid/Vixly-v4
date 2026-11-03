using Vixly.Platform.Application.Identity;
using Vixly.Platform.Application.Ports;

namespace Vixly.Platform.Application.Tests.Fakes;

// =========================================================
// FILE: FakeIdleTimes.cs
//
// PURPOSE:
// Control's inactivity times for tests, read from the fake security settings (owner, 2026-10-27).
//
// RESPONSIBILITIES:
// - Give the times of the settings row, or the defaults; count Forget calls
//
// IMPORTANT NOTES:
// - No caching: tests see a change at once
// =========================================================
internal sealed class FakeIdleTimes : IPlatformIdleTimesReader
{
    private readonly FakeSecuritySettings _settings;

    public FakeIdleTimes(FakeSecuritySettings settings)
    {
        _settings = settings;
    }

    public int Forgotten { get; private set; }

    public Task<PlatformIdleTimes> GetAsync(CancellationToken cancellationToken) => Task.FromResult(_settings.Settings is { } row
        ? new PlatformIdleTimes(TimeSpan.FromMinutes(row.IdleMinutes), TimeSpan.FromMinutes(row.IdleWarnMinutes))
        : PlatformIdleTimes.Default);

    public void Forget() => Forgotten++;
}
