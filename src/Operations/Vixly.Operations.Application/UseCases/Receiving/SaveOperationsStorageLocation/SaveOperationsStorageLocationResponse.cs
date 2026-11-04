using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Receiving.SaveOperationsStorageLocation;

// =========================================================
// FILE: SaveOperationsStorageLocationResponse.cs
//
// PURPOSE:
// Adds a shelf to a warehouse, or changes its zone or whether it is in use (Slice 3B).
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class SaveOperationsStorageLocationResponse
{
    private SaveOperationsStorageLocationResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public Guid LocationId { get; private init; }

    public static SaveOperationsStorageLocationResponse Success(Guid locationId)
    {
        return new SaveOperationsStorageLocationResponse { Succeeded = true, LocationId = locationId };
    }

    public static SaveOperationsStorageLocationResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new SaveOperationsStorageLocationResponse { Error = error };
    }
}
