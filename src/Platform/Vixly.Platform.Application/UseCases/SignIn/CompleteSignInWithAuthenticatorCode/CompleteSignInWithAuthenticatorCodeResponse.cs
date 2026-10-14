using Vixly.Shared.Kernel.Errors;

namespace Vixly.Platform.Application.UseCases.SignIn.CompleteSignInWithAuthenticatorCode;

// =========================================================
// FILE: CompleteSignInWithAuthenticatorCodeResponse.cs
//
// PURPOSE:
// Result of CompleteSignInWithAuthenticatorCode: the new session.
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - SessionToken goes into the session cookie only
// =========================================================
public sealed class CompleteSignInWithAuthenticatorCodeResponse
{
    private CompleteSignInWithAuthenticatorCodeResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public Guid SessionId { get; private init; }

    public string SessionToken { get; private init; } = string.Empty;

    public DateTimeOffset ExpiresUtc { get; private init; }

    public static CompleteSignInWithAuthenticatorCodeResponse Success(Guid sessionId, string sessionToken, DateTimeOffset expiresUtc)
    {
        return new CompleteSignInWithAuthenticatorCodeResponse { Succeeded = true, SessionId = sessionId, SessionToken = sessionToken, ExpiresUtc = expiresUtc };
    }

    public static CompleteSignInWithAuthenticatorCodeResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new CompleteSignInWithAuthenticatorCodeResponse { Error = error };
    }
}
