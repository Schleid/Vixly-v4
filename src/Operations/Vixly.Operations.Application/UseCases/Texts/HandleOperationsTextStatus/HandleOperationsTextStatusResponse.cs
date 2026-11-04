using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Texts.HandleOperationsTextStatus;

// =========================================================
// FILE: HandleOperationsTextStatusResponse.cs
//
// PURPOSE:
// Takes Twilio's report of what became of a text message (Slice 9; review).
//
// RESPONSIBILITIES:
// - Report success, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class HandleOperationsTextStatusResponse
{
    private HandleOperationsTextStatusResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public static HandleOperationsTextStatusResponse Success()
    {
        return new HandleOperationsTextStatusResponse { Succeeded = true };
    }

    public static HandleOperationsTextStatusResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new HandleOperationsTextStatusResponse { Error = error };
    }
}
