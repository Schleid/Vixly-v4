using Vixly.Operations.Application.Items;
using Vixly.Operations.Domain.Items;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Items.ListOperationsItemsToReview;

// =========================================================
// FILE: ListOperationsItemsToReviewResponse.cs
//
// PURPOSE:
// The parcels at origin whose items still need a customs code or a country of origin (owner, 2026-11-01).
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class ListOperationsItemsToReviewResponse
{
    private ListOperationsItemsToReviewResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public IReadOnlyList<OperationsItemReviewRow> Parcels { get; private init; } = Array.Empty<OperationsItemReviewRow>();

    public bool Required { get; private init; }

    public static ListOperationsItemsToReviewResponse Success(IReadOnlyList<OperationsItemReviewRow> parcels, bool required)
    {
        return new ListOperationsItemsToReviewResponse { Succeeded = true, Parcels = parcels, Required = required };
    }

    public static ListOperationsItemsToReviewResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new ListOperationsItemsToReviewResponse { Error = error };
    }
}
