using Vixly.Shared.Kernel.Errors;

namespace Vixly.Platform.Application.UseCases.SignIn.BeginSignIn;

// =========================================================
// FILE: BeginSignInResponse.cs
//
// PURPOSE:
// Result of the password step: a pending sign-in and what it still needs.
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - ChallengeToken goes into a short-lived HttpOnly cookie
// - Failure is always SignInFailed, whatever the cause
// =========================================================
public sealed class BeginSignInResponse
{
    private BeginSignInResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public string ChallengeToken { get; private init; } = string.Empty;

    public bool EnrolmentRequired { get; private init; }

    public bool HasPasskey { get; private init; }

    public bool HasAuthenticatorApp { get; private init; }

    public int RecoveryCodesLeft { get; private init; }

    public static BeginSignInResponse Success(string challengeToken, bool enrolmentRequired, bool hasPasskey, bool hasAuthenticatorApp, int recoveryCodesLeft)
    {
        return new BeginSignInResponse { Succeeded = true, ChallengeToken = challengeToken, EnrolmentRequired = enrolmentRequired, HasPasskey = hasPasskey, HasAuthenticatorApp = hasAuthenticatorApp, RecoveryCodesLeft = recoveryCodesLeft };
    }

    public static BeginSignInResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new BeginSignInResponse { Error = error };
    }
}
