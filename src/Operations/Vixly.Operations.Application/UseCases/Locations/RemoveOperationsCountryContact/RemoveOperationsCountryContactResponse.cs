using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Locations.RemoveOperationsCountryContact;

// =========================================================
// FILE: RemoveOperationsCountryContactResponse.cs
//
// PURPOSE:
// Removes a country contact (Slice 1B).
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class RemoveOperationsCountryContactResponse
{
    private RemoveOperationsCountryContactResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public static RemoveOperationsCountryContactResponse Success()
    {
        return new RemoveOperationsCountryContactResponse { Succeeded = true };
    }

    public static RemoveOperationsCountryContactResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new RemoveOperationsCountryContactResponse { Error = error };
    }
}
