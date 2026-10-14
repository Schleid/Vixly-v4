using Vixly.Shared.Kernel.Errors;

namespace Vixly.Platform.Application.UseCases.SignIn.CompleteSignInWithRecoveryCode;

// =========================================================
// FILE: CompleteSignInWithRecoveryCodeResponse.cs
//
// PURPOSE:
// Result of CompleteSignInWithRecoveryCode: the new session.
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - SessionToken goes into the session cookie only
// =========================================================
public sealed class CompleteSignInWithRecoveryCodeResponse
{
    private CompleteSignInWithRecoveryCodeResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public Guid SessionId { get; private init; }

    public string SessionToken { get; private init; } = string.Empty;

    public DateTimeOffset ExpiresUtc { get; private init; }

    public int RecoveryCodesLeft { get; private init; }

    public static CompleteSignInWithRecoveryCodeResponse Success(Guid sessionId, string sessionToken, DateTimeOffset expiresUtc, int recoveryCodesLeft)
    {
        return new CompleteSignInWithRecoveryCodeResponse { Succeeded = true, SessionId = sessionId, SessionToken = sessionToken, ExpiresUtc = expiresUtc, RecoveryCodesLeft = recoveryCodesLeft };
    }

    public static CompleteSignInWithRecoveryCodeResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new CompleteSignInWithRecoveryCodeResponse { Error = error };
    }
}
