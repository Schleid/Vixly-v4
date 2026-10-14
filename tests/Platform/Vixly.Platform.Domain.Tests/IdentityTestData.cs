using Vixly.Platform.Domain.Identity;
using Vixly.Shared.Kernel.Localization;
using Vixly.Shared.Kernel.ValueObjects;

namespace Vixly.Platform.Domain.Tests;

// =========================================================
// FILE: IdentityTestData.cs
//
// PURPOSE:
// Builders for identity tests.
//
// RESPONSIBILITIES:
// - Create invited and active staff users at a fixed time
//
// IMPORTANT NOTES:
// - Times are fixed UTC values; tests never read the clock
// =========================================================
internal static class IdentityTestData
{
    public static readonly DateTimeOffset Now = new(2026, 10, 13, 12, 0, 0, TimeSpan.Zero);

    public const string Hash = "$2a$12$abcdefghijklmnopqrstuuJ0tAZ9Ty3t7WIkpq1sGvXyH0P3QeVx6";

    public static PlatformUser Invited(PlatformUserAuthorityLevel level = PlatformUserAuthorityLevel.Standard)
    {
        return PlatformUser.Invite(
            "Ada",
            "Lovelace",
            EmailAddress.Create("ada@example.com"),
            LanguageCode.En,
            level,
            null,
            null,
            Now);
    }

    public static PlatformUser Active(PlatformUserAuthorityLevel level = PlatformUserAuthorityLevel.Standard)
    {
        PlatformUser user = Invited(level);
        user.AcceptInvitation(Hash, Now);

        return user;
    }
}
