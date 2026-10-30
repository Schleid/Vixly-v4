using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.SignIn.RequestOperationsPasswordReset;

// =========================================================
// FILE: RequestOperationsPasswordResetResponse.cs
//
// PURPOSE:
// Emails a password reset link to a staff account (Slice 1C, "Forgot your password?").
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class RequestOperationsPasswordResetResponse
{
    private RequestOperationsPasswordResetResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public static RequestOperationsPasswordResetResponse Success()
    {
        return new RequestOperationsPasswordResetResponse { Succeeded = true };
    }

    public static RequestOperationsPasswordResetResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new RequestOperationsPasswordResetResponse { Error = error };
    }
}
