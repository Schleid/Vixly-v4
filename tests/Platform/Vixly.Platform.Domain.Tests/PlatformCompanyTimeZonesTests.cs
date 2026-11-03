using System.Reflection;
using Vixly.Platform.Domain.Tenants;

namespace Vixly.Platform.Domain.Tests;

// =========================================================
// FILE: PlatformCompanyTimeZonesTests.cs
//
// PURPOSE:
// Proves the time zone a new company starts with (owner, 2026-11-02).
//
// RESPONSIBILITIES:
// - The signing-up browser's zone wins when it is a valid IANA id
// - Otherwise the state or province, then the country, then UTC
// - Every zone in the tables is known to this host
//
// IMPORTANT NOTES:
// - The tables are read by reflection so a new entry is checked too
// =========================================================
public sealed class PlatformCompanyTimeZonesTests
{
    private static PlatformPostalAddress Address(string country, string? state) => PlatformPostalAddress.Create("1 Road", null, "City", state, null, country);

    [Theory]
    [InlineData("America/Chicago", "US", "FL", "America/Chicago")]
    [InlineData(null, "US", "FL", "America/New_York")]
    [InlineData("", "US", "Florida", "America/New_York")]
    [InlineData("UTC", "US", "TX", "America/Chicago")]
    [InlineData("Eastern Standard Time", "US", "ca", "America/Los_Angeles")]
    [InlineData("Not/AZone", "US", "AZ", "America/Phoenix")]
    [InlineData(null, "US", null, "America/New_York")]
    [InlineData(null, "US", "Nowhere", "America/New_York")]
    [InlineData(null, "CA", "BC", "America/Vancouver")]
    [InlineData(null, "CA", "Québec", "America/Toronto")]
    [InlineData(null, "HT", "Ouest", "America/Port-au-Prince")]
    [InlineData(null, "FR", null, "Europe/Paris")]
    [InlineData(null, "JP", null, "UTC")]
    public void Choose_BrowserThenStateThenCountry(string? browser, string country, string? state, string expected)
    {
        Assert.Equal(expected, PlatformCompanyTimeZones.Choose(browser, Address(country, state)));
    }

    [Fact]
    public void Choose_WithoutAnAddress_IsUtc()
    {
        Assert.Equal("UTC", PlatformCompanyTimeZones.Choose(null, null));
    }

    [Fact]
    public void EveryZoneInTheTables_IsKnown()
    {
        FieldInfo[] tables = typeof(PlatformCompanyTimeZones).GetFields(BindingFlags.NonPublic | BindingFlags.Static);
        int checkedZones = 0;

        foreach (FieldInfo table in tables)
        {
            foreach (string zone in ((IReadOnlyDictionary<string, string>)table.GetValue(null)!).Values.Distinct(StringComparer.Ordinal))
            {
                Assert.True(TimeZoneInfo.TryFindSystemTimeZoneById(zone, out _), zone);
                Assert.Equal(zone, PlatformTimeZones.Normalize(zone));
                checkedZones++;
            }
        }

        Assert.True(checkedZones > 60);
    }
}
