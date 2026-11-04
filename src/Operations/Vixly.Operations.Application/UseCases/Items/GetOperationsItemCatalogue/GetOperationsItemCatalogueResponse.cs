using Vixly.Operations.Application.Items;
using Vixly.Operations.Domain.Items;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Items.GetOperationsItemCatalogue;

// =========================================================
// FILE: GetOperationsItemCatalogueResponse.cs
//
// PURPOSE:
// The company's item catalogue and whether items must be complete before bundling (owner, 2026-11-01).
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class GetOperationsItemCatalogueResponse
{
    private GetOperationsItemCatalogueResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public IReadOnlyList<OperationsItemCatalogueEntry> Entries { get; private init; } = Array.Empty<OperationsItemCatalogueEntry>();

    public DateTimeOffset? RequiredSinceUtc { get; private init; }

    public bool CanEdit { get; private init; }

    public static GetOperationsItemCatalogueResponse Success(IReadOnlyList<OperationsItemCatalogueEntry> entries, DateTimeOffset? requiredSinceUtc, bool canEdit)
    {
        return new GetOperationsItemCatalogueResponse { Succeeded = true, Entries = entries, RequiredSinceUtc = requiredSinceUtc, CanEdit = canEdit };
    }

    public static GetOperationsItemCatalogueResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new GetOperationsItemCatalogueResponse { Error = error };
    }
}
