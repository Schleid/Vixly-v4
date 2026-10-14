using Vixly.Platform.Domain.Identity;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Platform.Application.UseCases.Users;

// =========================================================
// FILE: PlatformUserTarget.cs
//
// PURPOSE:
// The staff user an administrator acts on, or why they cannot.
//
// RESPONSIBILITIES:
// - Carry the loaded user, or the error to report
//
// IMPORTANT NOTES:
// - Exactly one of User and Error is set
// =========================================================
public sealed record PlatformUserTarget(PlatformUser? User, Error? Error)
{
    public static PlatformUserTarget Found(PlatformUser user)
    {
        ArgumentNullException.ThrowIfNull(user);

        return new PlatformUserTarget(user, null);
    }

    public static PlatformUserTarget Refused(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new PlatformUserTarget(null, error);
    }
}
