using Vixly.Shared.Kernel.Errors;

namespace Vixly.Platform.Application.UseCases.SignIn.CompleteSignInEnrolment;

// =========================================================
// FILE: CompleteSignInEnrolmentResponse.cs
//
// PURPOSE:
// Result of enrolment: the session and the recovery codes, shown once.
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - RecoveryCodes are plain text here and nowhere else; the
//   console shows them once and asks the user to save them
// =========================================================
public sealed class CompleteSignInEnrolmentResponse
{
    private CompleteSignInEnrolmentResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public Guid SessionId { get; private init; }

    public string SessionToken { get; private init; } = string.Empty;

    public DateTimeOffset ExpiresUtc { get; private init; }

    public IReadOnlyList<string> RecoveryCodes { get; private init; } = Array.Empty<string>();

    public static CompleteSignInEnrolmentResponse Success(Guid sessionId, string sessionToken, DateTimeOffset expiresUtc, IReadOnlyList<string> recoveryCodes)
    {
        return new CompleteSignInEnrolmentResponse { Succeeded = true, SessionId = sessionId, SessionToken = sessionToken, ExpiresUtc = expiresUtc, RecoveryCodes = recoveryCodes };
    }

    public static CompleteSignInEnrolmentResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new CompleteSignInEnrolmentResponse { Error = error };
    }
}
