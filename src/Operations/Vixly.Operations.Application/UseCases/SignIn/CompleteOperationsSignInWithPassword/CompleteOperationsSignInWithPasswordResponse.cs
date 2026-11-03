using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.SignIn.CompleteOperationsSignInWithPassword;

// =========================================================
// FILE: CompleteOperationsSignInWithPasswordResponse.cs
//
// PURPOSE:
// Finishes the daily check of a badge or PIN sign-in with the password, for a person without an authenticator (Slice 12A).
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class CompleteOperationsSignInWithPasswordResponse
{
    private CompleteOperationsSignInWithPasswordResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public string SessionToken { get; private init; } = string.Empty;

    public static CompleteOperationsSignInWithPasswordResponse Success(string sessionToken)
    {
        return new CompleteOperationsSignInWithPasswordResponse { Succeeded = true, SessionToken = sessionToken };
    }

    public static CompleteOperationsSignInWithPasswordResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new CompleteOperationsSignInWithPasswordResponse { Error = error };
    }
}
