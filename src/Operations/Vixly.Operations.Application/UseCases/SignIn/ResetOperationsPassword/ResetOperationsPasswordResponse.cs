using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.SignIn.ResetOperationsPassword;

// =========================================================
// FILE: ResetOperationsPasswordResponse.cs
//
// PURPOSE:
// Sets a new password from an emailed reset link (Slice 1C).
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class ResetOperationsPasswordResponse
{
    private ResetOperationsPasswordResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public static ResetOperationsPasswordResponse Success()
    {
        return new ResetOperationsPasswordResponse { Succeeded = true };
    }

    public static ResetOperationsPasswordResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new ResetOperationsPasswordResponse { Error = error };
    }
}
