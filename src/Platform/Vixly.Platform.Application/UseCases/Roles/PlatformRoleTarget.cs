using Vixly.Platform.Domain.Identity;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Platform.Application.UseCases.Roles;

// =========================================================
// FILE: PlatformRoleTarget.cs
//
// PURPOSE:
// The role a role use case acts on, or why it may not.
//
// RESPONSIBILITIES:
// - Carry the loaded role or the refusal
//
// IMPORTANT NOTES:
// - Exactly one of Role and Error is set
// =========================================================
public sealed record PlatformRoleTarget(PlatformRole? Role, Error? Error)
{
    public static PlatformRoleTarget Found(PlatformRole role)
    {
        ArgumentNullException.ThrowIfNull(role);

        return new PlatformRoleTarget(role, null);
    }

    public static PlatformRoleTarget Refused(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new PlatformRoleTarget(null, error);
    }
}
