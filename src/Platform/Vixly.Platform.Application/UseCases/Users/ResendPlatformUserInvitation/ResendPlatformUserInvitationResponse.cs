using Vixly.Shared.Kernel.Errors;

namespace Vixly.Platform.Application.UseCases.Users.ResendPlatformUserInvitation;

// =========================================================
// FILE: ResendPlatformUserInvitationResponse.cs
//
// PURPOSE:
// Result of ResendPlatformUserInvitation.
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class ResendPlatformUserInvitationResponse
{
    private ResendPlatformUserInvitationResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public static ResendPlatformUserInvitationResponse Success()
    {
        return new ResendPlatformUserInvitationResponse { Succeeded = true };
    }

    public static ResendPlatformUserInvitationResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new ResendPlatformUserInvitationResponse { Error = error };
    }
}
