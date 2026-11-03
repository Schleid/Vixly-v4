using Vixly.Operations.Application.Items;
using Vixly.Operations.Domain.Items;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Items.RemoveOperationsItemCatalogueEntry;

// =========================================================
// FILE: RemoveOperationsItemCatalogueEntryResponse.cs
//
// PURPOSE:
// Removes an item from the company's catalogue (owner, 2026-11-01).
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class RemoveOperationsItemCatalogueEntryResponse
{
    private RemoveOperationsItemCatalogueEntryResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public static RemoveOperationsItemCatalogueEntryResponse Success()
    {
        return new RemoveOperationsItemCatalogueEntryResponse { Succeeded = true };
    }

    public static RemoveOperationsItemCatalogueEntryResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new RemoveOperationsItemCatalogueEntryResponse { Error = error };
    }
}
