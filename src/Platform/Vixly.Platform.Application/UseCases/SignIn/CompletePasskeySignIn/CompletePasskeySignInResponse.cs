using Vixly.Shared.Kernel.Errors;

namespace Vixly.Platform.Application.UseCases.SignIn.CompletePasskeySignIn;

// =========================================================
// FILE: CompletePasskeySignInResponse.cs
//
// PURPOSE:
// Result of CompletePasskeySignIn.
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// - Every failure answers the same sign_in_failed code, so the
//   page reveals nothing about accounts or passkeys
// =========================================================
public sealed class CompletePasskeySignInResponse
{
    private CompletePasskeySignInResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public Guid SessionId { get; private init; }

    public string SessionToken { get; private init; } = string.Empty;

    public DateTimeOffset ExpiresUtc { get; private init; }

    public static CompletePasskeySignInResponse Success(Guid sessionId, string sessionToken, DateTimeOffset expiresUtc)
    {
        return new CompletePasskeySignInResponse { Succeeded = true, SessionId = sessionId, SessionToken = sessionToken, ExpiresUtc = expiresUtc };
    }

    public static CompletePasskeySignInResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new CompletePasskeySignInResponse { Error = error };
    }
}
