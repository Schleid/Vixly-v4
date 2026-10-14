using Vixly.Shared.Kernel.Errors;

namespace Vixly.Platform.Application.UseCases.SignIn.RequestPasswordReset;

// =========================================================
// FILE: RequestPasswordResetResponse.cs
//
// PURPOSE:
// Result of a reset request. Always success.
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - The answer never says whether the email exists
// =========================================================
public sealed class RequestPasswordResetResponse
{
    private RequestPasswordResetResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public static RequestPasswordResetResponse Success()
    {
        return new RequestPasswordResetResponse { Succeeded = true };
    }

    public static RequestPasswordResetResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new RequestPasswordResetResponse { Error = error };
    }
}
