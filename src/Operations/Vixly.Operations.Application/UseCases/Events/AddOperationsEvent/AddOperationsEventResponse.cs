using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Events.AddOperationsEvent;

// =========================================================
// FILE: AddOperationsEventResponse.cs
//
// PURPOSE:
// Record a note, a delay or an exception by hand (Slice 6C, E-1 to E-5).
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class AddOperationsEventResponse
{
    private AddOperationsEventResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public int Parcels { get; private init; }

    public static AddOperationsEventResponse Success(int parcels)
    {
        return new AddOperationsEventResponse { Succeeded = true, Parcels = parcels };
    }

    public static AddOperationsEventResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new AddOperationsEventResponse { Error = error };
    }
}
