using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Locations.SaveOperationsCountryContact;

// =========================================================
// FILE: SaveOperationsCountryContactResponse.cs
//
// PURPOSE:
// Creates or changes how customers in one country reach the tenant (Slice 1B).
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class SaveOperationsCountryContactResponse
{
    private SaveOperationsCountryContactResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public Guid ContactId { get; private init; }

    public static SaveOperationsCountryContactResponse Success(Guid contactId)
    {
        return new SaveOperationsCountryContactResponse { Succeeded = true, ContactId = contactId };
    }

    public static SaveOperationsCountryContactResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new SaveOperationsCountryContactResponse { Error = error };
    }
}
