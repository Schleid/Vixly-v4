using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Inspection.SaveOperationsRestrictedItem;

// =========================================================
// FILE: SaveOperationsRestrictedItemResponse.cs
//
// PURPOSE:
// Adds an entry to the company's restricted-items list, or changes one (Slice 3C).
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class SaveOperationsRestrictedItemResponse
{
    private SaveOperationsRestrictedItemResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public Guid ItemId { get; private init; }

    public static SaveOperationsRestrictedItemResponse Success(Guid itemId)
    {
        return new SaveOperationsRestrictedItemResponse { Succeeded = true, ItemId = itemId };
    }

    public static SaveOperationsRestrictedItemResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new SaveOperationsRestrictedItemResponse { Error = error };
    }
}
