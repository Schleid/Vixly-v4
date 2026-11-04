using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.SignIn.SignOutOperations;

// =========================================================
// FILE: SignOutOperationsResponse.cs
//
// PURPOSE:
// Ends the current staff session, and optionally the others (Slice 1).
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class SignOutOperationsResponse
{
    private SignOutOperationsResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public static SignOutOperationsResponse Success()
    {
        return new SignOutOperationsResponse { Succeeded = true };
    }

    public static SignOutOperationsResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new SignOutOperationsResponse { Error = error };
    }
}
