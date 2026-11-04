using Vixly.Operations.Application.Items;
using Vixly.Operations.Domain.Items;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Items.SaveOperationsItemCatalogueEntry;

// =========================================================
// FILE: SaveOperationsItemCatalogueEntryResponse.cs
//
// PURPOSE:
// Adds or changes an item in the company's catalogue (owner, 2026-11-01).
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class SaveOperationsItemCatalogueEntryResponse
{
    private SaveOperationsItemCatalogueEntryResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public Guid EntryId { get; private init; }

    public static SaveOperationsItemCatalogueEntryResponse Success(Guid entryId)
    {
        return new SaveOperationsItemCatalogueEntryResponse { Succeeded = true, EntryId = entryId };
    }

    public static SaveOperationsItemCatalogueEntryResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new SaveOperationsItemCatalogueEntryResponse { Error = error };
    }
}
