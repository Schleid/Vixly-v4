using Vixly.Platform.Application.Queries;
using Vixly.Platform.Application.Security;
using Vixly.Platform.Domain.Identity;

namespace Vixly.Platform.Application.UseCases.Users;

// =========================================================
// FILE: PlatformUserAdminActionRules.cs
//
// PURPOSE:
// Decides which administrator actions the console offers for
// one staff user, so screens never guess.
//
// RESPONSIBILITIES:
// - Offer nothing on your own account or on a user with a higher
//   authority level
// - Offer an action only with its permission and when the
//   account state allows it
//
// IMPORTANT NOTES:
// - Offering is not authorizing: each use case still runs its
//   gate and the domain still enforces its rules
// - Mirrors PlatformUserAccount transitions; the tests pin
//   both
// =========================================================
public static class PlatformUserAdminActionRules
{
    public static IReadOnlyCollection<PlatformUserAdminAction> Available(
        PlatformUserActionFacts target,
        Guid actorUserId,
        PlatformPermissionSnapshot actor)
    {
        ArgumentNullException.ThrowIfNull(target);
        ArgumentNullException.ThrowIfNull(actor);

        List<PlatformUserAdminAction> actions = new();

        if (!actor.IsAccountActive
            || target.PlatformUserId == actorUserId
            || actor.AuthorityLevel < target.AuthorityLevel)
        {
            return actions;
        }

        bool Holds(string permission)
        {
            return actor.AuthorityLevel == PlatformUserAuthorityLevel.SuperUser
                || actor.PermissionCodes.Contains(permission);
        }

        PlatformUserAccountStatus status = target.Status;
        bool deactivated = status == PlatformUserAccountStatus.Deactivated;

        Add(actions, PlatformUserAdminAction.Lock, Holds(PlatformPermissions.UsersLock) && !deactivated && status != PlatformUserAccountStatus.Locked);
        Add(actions, PlatformUserAdminAction.Suspend, Holds(PlatformPermissions.UsersSuspend) && !deactivated && status != PlatformUserAccountStatus.Suspended);
        Add(actions, PlatformUserAdminAction.Deactivate, Holds(PlatformPermissions.UsersDeactivate) && !deactivated);
        Add(actions, PlatformUserAdminAction.Reactivate, Holds(PlatformPermissions.UsersReactivate)
            && status is PlatformUserAccountStatus.Suspended or PlatformUserAccountStatus.Locked or PlatformUserAccountStatus.Deactivated);
        Add(actions, PlatformUserAdminAction.ResetSecondFactor, Holds(PlatformPermissions.UsersResetSecondFactor) && target.HasSecondFactor);
        Add(actions, PlatformUserAdminAction.EndSessions, Holds(PlatformPermissions.UsersEndSessions) && target.OpenSessionCount > 0);
        Add(actions, PlatformUserAdminAction.ResendInvitation, Holds(PlatformPermissions.UsersInvite) && status == PlatformUserAccountStatus.Invited);
        Add(actions, PlatformUserAdminAction.SendPasswordReset, Holds(PlatformPermissions.UsersSendPasswordReset) && status == PlatformUserAccountStatus.Active);

        return actions;
    }

    private static void Add(List<PlatformUserAdminAction> actions, PlatformUserAdminAction action, bool allowed)
    {
        if (allowed)
        {
            actions.Add(action);
        }
    }
}
