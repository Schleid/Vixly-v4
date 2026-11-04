using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.SignIn.CompleteOperationsSignInWithRecoveryCode;

// =========================================================
// FILE: CompleteOperationsSignInWithRecoveryCodeResponse.cs
//
// PURPOSE:
// Finishes staff sign-in with a recovery code (Slice 1).
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class CompleteOperationsSignInWithRecoveryCodeResponse
{
    private CompleteOperationsSignInWithRecoveryCodeResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public string SessionToken { get; private init; } = string.Empty;

    public int RecoveryCodesLeft { get; private init; }

    public static CompleteOperationsSignInWithRecoveryCodeResponse Success(string sessionToken, int recoveryCodesLeft)
    {
        return new CompleteOperationsSignInWithRecoveryCodeResponse { Succeeded = true, SessionToken = sessionToken, RecoveryCodesLeft = recoveryCodesLeft };
    }

    public static CompleteOperationsSignInWithRecoveryCodeResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new CompleteOperationsSignInWithRecoveryCodeResponse { Error = error };
    }
}
