using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Items.SaveOperationsParcelItems;

// =========================================================
// FILE: SaveOperationsParcelItemsResponse.cs
//
// PURPOSE:
// Staff classify a parcel's items for customs: description on documents, customs code, country of origin, unit (owner, 2026-11-01).
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class SaveOperationsParcelItemsResponse
{
    private SaveOperationsParcelItemsResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public int Missing { get; private init; }

    public static SaveOperationsParcelItemsResponse Success(int missing)
    {
        return new SaveOperationsParcelItemsResponse { Succeeded = true, Missing = missing };
    }

    public static SaveOperationsParcelItemsResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new SaveOperationsParcelItemsResponse { Error = error };
    }
}
