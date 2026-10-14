using Vixly.Platform.Application.Queries;
using Vixly.Platform.Application.Security;
using Vixly.Platform.Application.UseCases.Users;
using Vixly.Platform.Domain.Identity;

namespace Vixly.Platform.Application.Tests;

// =========================================================
// FILE: PlatformUserAdminActionRulesTests.cs
//
// PURPOSE:
// Pins which administrator actions the console offers.
//
// RESPONSIBILITIES:
// - Nothing on your own account or on a higher authority
// - Each action needs its permission and a fitting state
//
// IMPORTANT NOTES:
// - Offering is not authorizing; the use cases are tested
//   separately
// =========================================================
public sealed class PlatformUserAdminActionRulesTests
{
    private static readonly Guid Actor = Guid.CreateVersion7();

    private static readonly Guid Target = Guid.CreateVersion7();

    [Fact]
    public void SuperUser_OnAnActiveEnrolledUserWithSessions_GetsEveryFittingAction()
    {
        IReadOnlyCollection<PlatformUserAdminAction> actions = Available(
            Facts(PlatformUserAccountStatus.Active),
            Snapshot(PlatformUserAuthorityLevel.SuperUser));

        Assert.Equal(
            new[]
            {
                PlatformUserAdminAction.Lock, PlatformUserAdminAction.Suspend, PlatformUserAdminAction.Deactivate,
                PlatformUserAdminAction.ResetSecondFactor, PlatformUserAdminAction.EndSessions, PlatformUserAdminAction.SendPasswordReset
            },
            actions);
    }

    [Theory]
    [InlineData(PlatformUserAccountStatus.Invited, new[] { PlatformUserAdminAction.Lock, PlatformUserAdminAction.Suspend, PlatformUserAdminAction.Deactivate, PlatformUserAdminAction.ResendInvitation })]
    [InlineData(PlatformUserAccountStatus.Locked, new[] { PlatformUserAdminAction.Suspend, PlatformUserAdminAction.Deactivate, PlatformUserAdminAction.Reactivate })]
    [InlineData(PlatformUserAccountStatus.Deactivated, new[] { PlatformUserAdminAction.Reactivate })]
    public void State_DecidesTheLifecycleActions(PlatformUserAccountStatus status, PlatformUserAdminAction[] expected)
    {
        IReadOnlyCollection<PlatformUserAdminAction> actions = Available(
            Facts(status, hasSecondFactor: false, sessions: 0),
            Snapshot(PlatformUserAuthorityLevel.SuperUser));

        Assert.Equal(expected, actions);
    }

    [Fact]
    public void OwnAccount_HigherAuthority_OrInactiveActor_GetNothing()
    {
        PlatformUserActionFacts self = Facts(PlatformUserAccountStatus.Active) with { PlatformUserId = Actor };
        PlatformUserActionFacts superUser = Facts(PlatformUserAccountStatus.Active) with { AuthorityLevel = PlatformUserAuthorityLevel.SuperUser };

        Assert.Empty(Available(self, Snapshot(PlatformUserAuthorityLevel.SuperUser)));
        Assert.Empty(Available(superUser, Snapshot(PlatformUserAuthorityLevel.Administrator, PlatformPermissions.All.ToArray())));
        Assert.Empty(Available(Facts(PlatformUserAccountStatus.Active), Snapshot(PlatformUserAuthorityLevel.SuperUser) with { IsAccountActive = false }));
    }

    [Fact]
    public void Permissions_DecideTheRest()
    {
        IReadOnlyCollection<PlatformUserAdminAction> actions = Available(
            Facts(PlatformUserAccountStatus.Active),
            Snapshot(PlatformUserAuthorityLevel.Administrator, PlatformPermissions.UsersLock, PlatformPermissions.UsersEndSessions));

        Assert.Equal(new[] { PlatformUserAdminAction.Lock, PlatformUserAdminAction.EndSessions }, actions);
    }

    private static IReadOnlyCollection<PlatformUserAdminAction> Available(PlatformUserActionFacts target, PlatformPermissionSnapshot actor)
    {
        return PlatformUserAdminActionRules.Available(target, Actor, actor);
    }

    private static PlatformUserActionFacts Facts(PlatformUserAccountStatus status, bool hasSecondFactor = true, int sessions = 2)
    {
        return new PlatformUserActionFacts(Target, status, PlatformUserAuthorityLevel.Standard, hasSecondFactor, sessions);
    }

    private static PlatformPermissionSnapshot Snapshot(PlatformUserAuthorityLevel level, params string[] codes)
    {
        return new PlatformPermissionSnapshot(true, level, codes.ToHashSet(StringComparer.Ordinal));
    }
}
