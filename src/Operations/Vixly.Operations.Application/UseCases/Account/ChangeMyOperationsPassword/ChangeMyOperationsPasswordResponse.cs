using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Account.ChangeMyOperationsPassword;

// =========================================================
// FILE: ChangeMyOperationsPasswordResponse.cs
//
// PURPOSE:
// Changes my password; every other session of mine ends (Slice 1D).
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class ChangeMyOperationsPasswordResponse
{
    private ChangeMyOperationsPasswordResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public static ChangeMyOperationsPasswordResponse Success()
    {
        return new ChangeMyOperationsPasswordResponse { Succeeded = true };
    }

    public static ChangeMyOperationsPasswordResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new ChangeMyOperationsPasswordResponse { Error = error };
    }
}
