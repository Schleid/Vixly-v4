using Vixly.Shared.Kernel.Errors;

namespace Vixly.Platform.Application.UseCases.SignIn.AcceptInvitation;

// =========================================================
// FILE: AcceptInvitationResponse.cs
//
// PURPOSE:
// Result of accepting an invitation: a pending sign-in that leads to enrolment.
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - The next step is always enrolment of a second factor
// =========================================================
public sealed class AcceptInvitationResponse
{
    private AcceptInvitationResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public string ChallengeToken { get; private init; } = string.Empty;

    public static AcceptInvitationResponse Success(string challengeToken)
    {
        return new AcceptInvitationResponse { Succeeded = true, ChallengeToken = challengeToken };
    }

    public static AcceptInvitationResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new AcceptInvitationResponse { Error = error };
    }
}
