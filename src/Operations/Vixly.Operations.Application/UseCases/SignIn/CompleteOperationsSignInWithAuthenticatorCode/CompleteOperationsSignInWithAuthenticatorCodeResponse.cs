using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.SignIn.CompleteOperationsSignInWithAuthenticatorCode;

// =========================================================
// FILE: CompleteOperationsSignInWithAuthenticatorCodeResponse.cs
//
// PURPOSE:
// Finishes staff sign-in with an authenticator app code (Slice 1).
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class CompleteOperationsSignInWithAuthenticatorCodeResponse
{
    private CompleteOperationsSignInWithAuthenticatorCodeResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public string SessionToken { get; private init; } = string.Empty;

    public static CompleteOperationsSignInWithAuthenticatorCodeResponse Success(string sessionToken)
    {
        return new CompleteOperationsSignInWithAuthenticatorCodeResponse { Succeeded = true, SessionToken = sessionToken };
    }

    public static CompleteOperationsSignInWithAuthenticatorCodeResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new CompleteOperationsSignInWithAuthenticatorCodeResponse { Error = error };
    }
}
