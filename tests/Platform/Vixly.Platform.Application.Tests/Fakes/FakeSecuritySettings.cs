using Vixly.Platform.Application.Ports;
using Vixly.Platform.Domain.Identity;

namespace Vixly.Platform.Application.Tests.Fakes;

// =========================================================
// FILE: FakeSecuritySettings.cs
//
// PURPOSE:
// In-memory Require MFA for the use case tests; no row means
// Yes, as in the database.
//
// RESPONSIBILITIES:
// - Carry the values named above, nothing more
//
// IMPORTANT NOTES:
// - Part of Require MFA (owner, 2026-10-22)
// =========================================================
internal sealed class FakeSecuritySettings : IPlatformSecuritySettingsRepository
{
    public PlatformSecuritySettings? Settings { get; private set; }

    public static FakeSecuritySettings WithRequireMfa(bool requireMfa, Guid actorUserId, DateTimeOffset utcNow)
    {
        FakeSecuritySettings fake = new();
        fake.Settings = PlatformSecuritySettings.Default(utcNow);
        fake.Settings.ChangeRequireMfa(requireMfa, actorUserId, utcNow);

        return fake;
    }

    public Task<PlatformSecuritySettings?> GetAsync(CancellationToken cancellationToken) => Task.FromResult(Settings);

    public void Add(PlatformSecuritySettings settings) => Settings = settings;
}
