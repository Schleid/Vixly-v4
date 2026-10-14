using Vixly.Platform.Domain.Identity;
using Vixly.Shared.Kernel.Domain;
using Vixly.Shared.Kernel.Errors;
using static Vixly.Platform.Domain.Tests.IdentityTestData;

namespace Vixly.Platform.Domain.Tests;

// =========================================================
// FILE: PlatformPasskeyCeremonyTests.cs
//
// PURPOSE:
// Proves the passkey ceremony state.
//
// RESPONSIBILITIES:
// - Purpose and owner rules at start
// - Single use, purpose and owner bound, 5-minute expiry
//
// IMPORTANT NOTES:
// - Tests assert error codes, never text (L-1)
// =========================================================
public sealed class PlatformPasskeyCeremonyTests
{
    private static readonly string TokenHash = new('c', 64);

    private static readonly Guid User = Guid.CreateVersion7(Now);

    [Fact]
    public void SignIn_HasNoUser_AndTheOthersNeedOne()
    {
        AssertError(PlatformIdentityErrors.PasskeyCeremonyInvalid, () =>
            PlatformPasskeyCeremony.Start(TokenHash, PlatformPasskeyCeremonyPurpose.SignIn, User, "{}", Now));
        AssertError(PlatformIdentityErrors.PasskeyCeremonyInvalid, () =>
            PlatformPasskeyCeremony.Start(TokenHash, PlatformPasskeyCeremonyPurpose.Registration, null, "{}", Now));
        AssertError(PlatformIdentityErrors.PasskeyCeremonyInvalid, () =>
            PlatformPasskeyCeremony.Start(TokenHash, PlatformPasskeyCeremonyPurpose.StepUp, User, " ", Now));
    }

    [Fact]
    public void Ceremony_IsSingleUse_PurposeAndOwnerBound_AndExpires()
    {
        PlatformPasskeyCeremony ceremony = PlatformPasskeyCeremony.Start(TokenHash, PlatformPasskeyCeremonyPurpose.StepUp, User, "{}", Now);

        Assert.False(ceremony.IsUsableFor(PlatformPasskeyCeremonyPurpose.Registration, User, Now));
        Assert.False(ceremony.IsUsableFor(PlatformPasskeyCeremonyPurpose.StepUp, Guid.CreateVersion7(Now), Now));
        Assert.False(ceremony.IsUsableFor(PlatformPasskeyCeremonyPurpose.StepUp, User, Now.AddMinutes(5)));
        Assert.True(ceremony.IsUsableFor(PlatformPasskeyCeremonyPurpose.StepUp, User, Now.AddMinutes(4)));

        ceremony.Complete(PlatformPasskeyCeremonyPurpose.StepUp, User, Now.AddMinutes(1));

        AssertError(PlatformIdentityErrors.PasskeyCeremonyInvalid, () =>
            ceremony.Complete(PlatformPasskeyCeremonyPurpose.StepUp, User, Now.AddMinutes(1)));
    }

    private static void AssertError(Error expected, Action action)
    {
        DomainException exception = Assert.Throws<DomainException>(action);
        Assert.Equal(expected, exception.Error);
    }
}
