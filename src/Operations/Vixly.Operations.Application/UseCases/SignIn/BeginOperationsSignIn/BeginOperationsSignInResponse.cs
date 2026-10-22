using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.SignIn.BeginOperationsSignIn;

// =========================================================
// FILE: BeginOperationsSignInResponse.cs
//
// PURPOSE:
// Checks the email and password of a staff member (Slice 1).
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class BeginOperationsSignInResponse
{
    private BeginOperationsSignInResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public string ChallengeToken { get; private init; } = string.Empty;

    public bool EnrolmentRequired { get; private init; }

    public string SessionToken { get; private init; } = string.Empty;

    public static BeginOperationsSignInResponse Success(string challengeToken, bool enrolmentRequired, string sessionToken)
    {
        return new BeginOperationsSignInResponse { Succeeded = true, ChallengeToken = challengeToken, EnrolmentRequired = enrolmentRequired, SessionToken = sessionToken };
    }

    public static BeginOperationsSignInResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new BeginOperationsSignInResponse { Error = error };
    }
}
