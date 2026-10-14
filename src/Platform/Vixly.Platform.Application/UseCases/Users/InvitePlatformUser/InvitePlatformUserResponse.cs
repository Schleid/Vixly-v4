using Vixly.Shared.Kernel.Errors;

namespace Vixly.Platform.Application.UseCases.Users.InvitePlatformUser;

// =========================================================
// FILE: InvitePlatformUserResponse.cs
//
// PURPOSE:
// Result of InvitePlatformUser: the new user id.
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - The invitation email is sent after the commit
// =========================================================
public sealed class InvitePlatformUserResponse
{
    private InvitePlatformUserResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public Guid PlatformUserId { get; private init; }

    public static InvitePlatformUserResponse Success(Guid platformUserId)
    {
        return new InvitePlatformUserResponse { Succeeded = true, PlatformUserId = platformUserId };
    }

    public static InvitePlatformUserResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new InvitePlatformUserResponse { Error = error };
    }
}
