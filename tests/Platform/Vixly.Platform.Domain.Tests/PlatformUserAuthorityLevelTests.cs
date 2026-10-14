using Vixly.Platform.Domain.Identity;

namespace Vixly.Platform.Domain.Tests;

// =========================================================
// FILE: PlatformUserAuthorityLevelTests.cs
//
// PURPOSE:
// Pins the persisted numeric values of the authority levels.
//
// RESPONSIBILITIES:
// - Fail the build if a value is renumbered or reordered
//
// IMPORTANT NOTES:
// - These numbers are stored in the database and compared for
//   ordering. A change needs a migration and an ADR
// =========================================================
public sealed class PlatformUserAuthorityLevelTests
{
    [Fact]
    public void Values_AreStableAndOrdered()
    {
        Assert.Equal(0, (int)PlatformUserAuthorityLevel.Standard);
        Assert.Equal(1, (int)PlatformUserAuthorityLevel.Manager);
        Assert.Equal(2, (int)PlatformUserAuthorityLevel.Administrator);
        Assert.Equal(3, (int)PlatformUserAuthorityLevel.SuperUser);
    }
}
