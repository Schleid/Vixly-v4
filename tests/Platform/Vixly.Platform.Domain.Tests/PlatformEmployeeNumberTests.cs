using Vixly.Platform.Domain.Identity;

namespace Vixly.Platform.Domain.Tests;

// =========================================================
// FILE: PlatformEmployeeNumberTests.cs
//
// PURPOSE:
// Proves the Control staff employee number format (owner, 2026-10-25).
//
// RESPONSIBILITIES:
// - TC- and at least four digits; numbers past 9999 keep growing
// - No number below 1
//
// IMPORTANT NOTES:
// - The numbers themselves come from the database sequence (Infrastructure tests)
// =========================================================
public sealed class PlatformEmployeeNumberTests
{
    [Theory]
    [InlineData(1, "TC-0001")]
    [InlineData(42, "TC-0042")]
    [InlineData(9999, "TC-9999")]
    [InlineData(10000, "TC-10000")]
    public void Format_PadsToFourDigits_AndKeepsGrowing(long number, string expected)
    {
        Assert.Equal(expected, PlatformEmployeeNumber.Format(number));
    }

    [Fact]
    public void Format_RefusesZero()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => PlatformEmployeeNumber.Format(0));
    }
}
