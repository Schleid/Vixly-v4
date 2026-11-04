using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Charges.SaveOperationsItemType;

// =========================================================
// FILE: SaveOperationsItemTypeResponse.cs
//
// PURPOSE:
// Adds one of the company's item types or changes one: a barrel, a car, a TV (Slice 4B design section 7).
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class SaveOperationsItemTypeResponse
{
    private SaveOperationsItemTypeResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public Guid ItemTypeId { get; private init; }

    public static SaveOperationsItemTypeResponse Success(Guid itemTypeId)
    {
        return new SaveOperationsItemTypeResponse { Succeeded = true, ItemTypeId = itemTypeId };
    }

    public static SaveOperationsItemTypeResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new SaveOperationsItemTypeResponse { Error = error };
    }
}
