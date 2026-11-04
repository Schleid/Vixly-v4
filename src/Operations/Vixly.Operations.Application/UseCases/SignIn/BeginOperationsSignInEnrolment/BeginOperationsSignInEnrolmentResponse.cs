using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.SignIn.BeginOperationsSignInEnrolment;

// =========================================================
// FILE: BeginOperationsSignInEnrolmentResponse.cs
//
// PURPOSE:
// Starts authenticator app enrolment during sign-in, for staff who need a second factor and have none (Slice 1).
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class BeginOperationsSignInEnrolmentResponse
{
    private BeginOperationsSignInEnrolmentResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public string ManualKey { get; private init; } = string.Empty;

    public string OtpAuthUri { get; private init; } = string.Empty;

    public static BeginOperationsSignInEnrolmentResponse Success(string manualKey, string otpAuthUri)
    {
        return new BeginOperationsSignInEnrolmentResponse { Succeeded = true, ManualKey = manualKey, OtpAuthUri = otpAuthUri };
    }

    public static BeginOperationsSignInEnrolmentResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new BeginOperationsSignInEnrolmentResponse { Error = error };
    }
}
