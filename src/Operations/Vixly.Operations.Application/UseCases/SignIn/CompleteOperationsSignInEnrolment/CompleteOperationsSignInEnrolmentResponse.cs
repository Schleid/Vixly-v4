using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.SignIn.CompleteOperationsSignInEnrolment;

// =========================================================
// FILE: CompleteOperationsSignInEnrolmentResponse.cs
//
// PURPOSE:
// Confirms the authenticator app, issues ten recovery codes and starts the session (Slice 1).
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class CompleteOperationsSignInEnrolmentResponse
{
    private CompleteOperationsSignInEnrolmentResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public string SessionToken { get; private init; } = string.Empty;

    public IReadOnlyList<string> RecoveryCodes { get; private init; } = Array.Empty<string>();

    public static CompleteOperationsSignInEnrolmentResponse Success(string sessionToken, IReadOnlyList<string> recoveryCodes)
    {
        return new CompleteOperationsSignInEnrolmentResponse { Succeeded = true, SessionToken = sessionToken, RecoveryCodes = recoveryCodes };
    }

    public static CompleteOperationsSignInEnrolmentResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new CompleteOperationsSignInEnrolmentResponse { Error = error };
    }
}
