using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Account.SetMyOperationsPin;

// =========================================================
// FILE: SetMyOperationsPinResponse.cs
//
// PURPOSE:
// Sets or changes the signed-in person's 4-digit PIN for licensed devices (Slice 12A, owner H-8).
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class SetMyOperationsPinResponse
{
    private SetMyOperationsPinResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public static SetMyOperationsPinResponse Success()
    {
        return new SetMyOperationsPinResponse { Succeeded = true };
    }

    public static SetMyOperationsPinResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new SetMyOperationsPinResponse { Error = error };
    }
}
