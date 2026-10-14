using Vixly.Shared.Kernel.Errors;

namespace Vixly.Platform.Application.UseCases.SignIn.CompletePasskeyEnrolment;

// =========================================================
// FILE: CompletePasskeyEnrolmentResponse.cs
//
// PURPOSE:
// Result of CompletePasskeyEnrolment.
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class CompletePasskeyEnrolmentResponse
{
    private CompletePasskeyEnrolmentResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public Guid SessionId { get; private init; }

    public string SessionToken { get; private init; } = string.Empty;

    public DateTimeOffset ExpiresUtc { get; private init; }

    public IReadOnlyList<string> RecoveryCodes { get; private init; } = Array.Empty<string>();

    public static CompletePasskeyEnrolmentResponse Success(Guid sessionId, string sessionToken, DateTimeOffset expiresUtc, IReadOnlyList<string> recoveryCodes)
    {
        return new CompletePasskeyEnrolmentResponse { Succeeded = true, SessionId = sessionId, SessionToken = sessionToken, ExpiresUtc = expiresUtc, RecoveryCodes = recoveryCodes };
    }

    public static CompletePasskeyEnrolmentResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new CompletePasskeyEnrolmentResponse { Error = error };
    }
}
