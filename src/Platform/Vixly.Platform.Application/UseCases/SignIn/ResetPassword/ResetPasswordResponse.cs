using Vixly.Shared.Kernel.Errors;

namespace Vixly.Platform.Application.UseCases.SignIn.ResetPassword;

// =========================================================
// FILE: ResetPasswordResponse.cs
//
// PURPOSE:
// Result of a password reset.
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - On success the user signs in again with the new password
// =========================================================
public sealed class ResetPasswordResponse
{
    private ResetPasswordResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public static ResetPasswordResponse Success()
    {
        return new ResetPasswordResponse { Succeeded = true };
    }

    public static ResetPasswordResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new ResetPasswordResponse { Error = error };
    }
}
