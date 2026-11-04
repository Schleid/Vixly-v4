using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.SignIn.AcceptOperationsInvitation;

// =========================================================
// FILE: AcceptOperationsInvitationResponse.cs
//
// PURPOSE:
// Sets the password from an invitation link and continues to the second factor (Slice 1).
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class AcceptOperationsInvitationResponse
{
    private AcceptOperationsInvitationResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public string ChallengeToken { get; private init; } = string.Empty;

    public bool EnrolmentRequired { get; private init; }

    public string SessionToken { get; private init; } = string.Empty;

    public static AcceptOperationsInvitationResponse Success(string challengeToken, bool enrolmentRequired, string sessionToken)
    {
        return new AcceptOperationsInvitationResponse { Succeeded = true, ChallengeToken = challengeToken, EnrolmentRequired = enrolmentRequired, SessionToken = sessionToken };
    }

    public static AcceptOperationsInvitationResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new AcceptOperationsInvitationResponse { Error = error };
    }
}
