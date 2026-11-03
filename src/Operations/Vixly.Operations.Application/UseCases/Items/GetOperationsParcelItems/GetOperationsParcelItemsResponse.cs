using Vixly.Operations.Application.Items;
using Vixly.Operations.Domain.Items;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Items.GetOperationsParcelItems;

// =========================================================
// FILE: GetOperationsParcelItemsResponse.cs
//
// PURPOSE:
// The items of a parcel, for staff to classify for customs (owner, 2026-11-01).
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class GetOperationsParcelItemsResponse
{
    private GetOperationsParcelItemsResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public IReadOnlyList<OperationsItemRow> Items { get; private init; } = Array.Empty<OperationsItemRow>();

    public IReadOnlyList<OperationsItemCatalogueEntry> Catalogue { get; private init; } = Array.Empty<OperationsItemCatalogueEntry>();

    public bool CanEdit { get; private init; }

    public int Missing { get; private init; }

    public static GetOperationsParcelItemsResponse Success(IReadOnlyList<OperationsItemRow> items, IReadOnlyList<OperationsItemCatalogueEntry> catalogue, bool canEdit)
    {
        return new GetOperationsParcelItemsResponse { Succeeded = true, Items = items, Catalogue = catalogue, CanEdit = canEdit, Missing = items.Count(item => !item.Complete) };
    }

    public static GetOperationsParcelItemsResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new GetOperationsParcelItemsResponse { Error = error };
    }
}
