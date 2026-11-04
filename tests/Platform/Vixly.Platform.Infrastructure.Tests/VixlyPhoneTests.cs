using Vixly.Shared.Localization;

namespace Vixly.Platform.Infrastructure.Tests;

// =========================================================
// FILE: VixlyPhoneTests.cs
//
// PURPOSE:
// Proves phone numbers are shown in international form (Shared.Localization, owner 2026-10-25).
//
// RESPONSIBILITIES:
// - Numbers saved with their code, without the plus, or as dialled at home, all read the same way
// - Numbers that cannot be read are shown as saved
//
// IMPORTANT NOTES:
// - Display only: nothing here changes what is saved
// =========================================================
public sealed class VixlyPhoneTests
{
    [Theory]
    [InlineData("+18555480504", null, "+1 855-548-0504")]
    [InlineData("18555480504", null, "+1 855-548-0504")]
    [InlineData("8555480504", null, "+1 855-548-0504")]
    [InlineData("8555480504", "US", "+1 855-548-0504")]
    [InlineData("+50941376609", null, "+509 4137-6609")]
    [InlineData("509 4137 6609", "HT", "+509 4137-6609")]
    [InlineData("41376609", "HT", "+509 4137-6609")]
    [InlineData("0612345678", "FR", "+33 6 12 34 56 78")]
    [InlineData("+33612345678", null, "+33 6 12 34 56 78")]
    [InlineData("+525512345678", null, "+52 55 1234 5678")]
    [InlineData("+4915123456789", null, "+49 151 234 567 89")]
    [InlineData("0049 151 23456789", null, "+49 151 234 567 89")]
    public void International_ReadsTheSameWhicheverWayItWasSaved(string stored, string? country, string expected)
    {
        Assert.Equal(expected, VixlyPhone.International(stored, country));
    }

    [Theory]
    [InlineData(null, "")]
    [InlineData("   ", "")]
    [InlineData("41376609", "41376609")]
    [InlineData("call us", "call us")]
    public void International_LeavesWhatItCannotRead(string? stored, string expected)
    {
        Assert.Equal(expected, VixlyPhone.International(stored));
    }
}
