using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Account.EndMyOperationsSession;

// =========================================================
// FILE: EndMyOperationsSessionResponse.cs
//
// PURPOSE:
// Ends one of my sessions, for example on a lost phone (Slice 1D).
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class EndMyOperationsSessionResponse
{
    private EndMyOperationsSessionResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public static EndMyOperationsSessionResponse Success()
    {
        return new EndMyOperationsSessionResponse { Succeeded = true };
    }

    public static EndMyOperationsSessionResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new EndMyOperationsSessionResponse { Error = error };
    }
}
