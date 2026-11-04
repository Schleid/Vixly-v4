using Vixly.Platform.Domain.Identity;
using Vixly.Shared.Kernel.Domain;
using static Vixly.Platform.Domain.Tests.IdentityTestData;

namespace Vixly.Platform.Domain.Tests;

// =========================================================
// FILE: PlatformIdleSettingsDomainTests.cs
//
// PURPOSE:
// Proves Control's inactivity sign-out setting (owner, 2026-10-27).
//
// RESPONSIBILITIES:
// - The default is the old fixed time: 5 minutes, the warning 1 minute before
// - The ranges, and who changed it
// - Sessions end at the time given
//
// IMPORTANT NOTES:
// - No database
// =========================================================
public sealed class PlatformIdleSettingsDomainTests
{
    private static readonly string TokenHash = new('a', 64);

    private static readonly Guid User = Guid.CreateVersion7(Now);

    [Fact]
    public void Default_IsFiveMinutes_WarnedOneMinuteBefore()
    {
        PlatformSecuritySettings settings = PlatformSecuritySettings.Default(Now);

        Assert.Equal((5, 1), (settings.IdleMinutes, settings.IdleWarnMinutes));
    }

    [Theory]
    [InlineData(4, 1, "platform.identity.idle_invalid")]
    [InlineData(31, 1, "platform.identity.idle_invalid")]
    [InlineData(10, 0, "platform.identity.idle_warn_invalid")]
    [InlineData(10, 6, "platform.identity.idle_warn_invalid")]
    [InlineData(5, 5, "platform.identity.idle_warn_invalid")]
    public void Times_OutsideTheirRange_AreRefused(int idle, int warn, string code)
    {
        PlatformSecuritySettings settings = PlatformSecuritySettings.Default(Now);

        Assert.Equal(code, Assert.Throws<DomainException>(() => settings.ChangeIdle(idle, warn, Guid.NewGuid(), Now)).Error.Code);
    }

    [Fact]
    public void Times_InRange_AreKept_WithWhoChangedThem()
    {
        PlatformSecuritySettings settings = PlatformSecuritySettings.Default(Now);
        Guid actor = Guid.NewGuid();

        settings.ChangeIdle(20, 3, actor, Now);

        Assert.Equal((20, 3, (Guid?)actor), (settings.IdleMinutes, settings.IdleWarnMinutes, settings.UpdatedByUserId));
    }

    [Fact]
    public void Sessions_EndAtTheTimeGiven()
    {
        PlatformUserSession session = PlatformUserSession.Start(User, TokenHash, null, null, Now);

        Assert.True(session.IsValidAt(Now.AddMinutes(15), TimeSpan.FromMinutes(20)));
        Assert.False(session.IsValidAt(Now.AddMinutes(25), TimeSpan.FromMinutes(20)));
    }
}
