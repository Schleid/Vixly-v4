using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.SignIn.BeginOperationsQuickSignIn;

// =========================================================
// FILE: BeginOperationsQuickSignInResponse.cs
//
// PURPOSE:
// Signs a staff member in with their badge or PIN on a licensed device of the company (Slice 12A, owner H-8).
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class BeginOperationsQuickSignInResponse
{
    private BeginOperationsQuickSignInResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public string SessionToken { get; private init; } = string.Empty;

    public string ChallengeToken { get; private init; } = string.Empty;

    public bool PasswordCheck { get; private init; }

    public static BeginOperationsQuickSignInResponse Success(string sessionToken, string challengeToken, bool passwordCheck)
    {
        return new BeginOperationsQuickSignInResponse { Succeeded = true, SessionToken = sessionToken, ChallengeToken = challengeToken, PasswordCheck = passwordCheck };
    }

    public static BeginOperationsQuickSignInResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new BeginOperationsQuickSignInResponse { Error = error };
    }
}
