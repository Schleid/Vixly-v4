using Vixly.Shared.Kernel.Errors;

namespace Vixly.Platform.Application.UseCases.SignIn.BeginSignInEnrolment;

// =========================================================
// FILE: BeginSignInEnrolmentResponse.cs
//
// PURPOSE:
// The authenticator-app setup to show: manual key and QR URI.
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Shown once; contains the plain secret, never logged
// =========================================================
public sealed class BeginSignInEnrolmentResponse
{
    private BeginSignInEnrolmentResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public string ManualKey { get; private init; } = string.Empty;

    public string OtpAuthUri { get; private init; } = string.Empty;

    public static BeginSignInEnrolmentResponse Success(string manualKey, string otpAuthUri)
    {
        return new BeginSignInEnrolmentResponse { Succeeded = true, ManualKey = manualKey, OtpAuthUri = otpAuthUri };
    }

    public static BeginSignInEnrolmentResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new BeginSignInEnrolmentResponse { Error = error };
    }
}
