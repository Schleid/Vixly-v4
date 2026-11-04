using Vixly.Operations.Application.Inspection;
using Vixly.Operations.Domain.Locations;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Inspection.ListOperationsHolds;

// =========================================================
// FILE: ListOperationsHoldsResponse.cs
//
// PURPOSE:
// Open holds at the warehouses a person covers, oldest first, and the disposals awaiting approval (Slice 3C design section 6).
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class ListOperationsHoldsResponse
{
    private ListOperationsHoldsResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public IReadOnlyList<OperationsHeldParcel> Items { get; private init; } = Array.Empty<OperationsHeldParcel>();

    public int TotalCount { get; private init; }

    public IReadOnlyList<OperationsWarehouse> Warehouses { get; private init; } = Array.Empty<OperationsWarehouse>();

    public IReadOnlyList<OperationsInspectionQueueItem> Approvals { get; private init; } = Array.Empty<OperationsInspectionQueueItem>();

    public bool CanApprove { get; private init; }

    // Slice 3E: packages waiting for their customer past the company's days, for the Owner or an administrator to decide.
    public IReadOnlyList<OperationsHeldParcel> Unclaimed { get; private init; } = Array.Empty<OperationsHeldParcel>();

    public int UnclaimedCount { get; private init; }

    public int UnclaimedDays { get; private init; }

    // Slice 4D step 3: the free days and the storage fee per day, set by the Owner or an administrator.
    public int StorageFreeDays { get; private init; }

    public decimal? StorageFeePerDay { get; private init; }

    public string CurrencyCode { get; private init; } = string.Empty;

    public ListOperationsHoldsResponse WithStorage(int freeDays, decimal? feePerDay, string currencyCode) =>
        new()
        {
            Succeeded = Succeeded, Error = Error, Items = Items, TotalCount = TotalCount, Warehouses = Warehouses, Approvals = Approvals, CanApprove = CanApprove,
            Unclaimed = Unclaimed, UnclaimedCount = UnclaimedCount, UnclaimedDays = UnclaimedDays, StorageFreeDays = freeDays, StorageFeePerDay = feePerDay, CurrencyCode = currencyCode
        };

    public static ListOperationsHoldsResponse Success(IReadOnlyList<OperationsHeldParcel> items, int totalCount, IReadOnlyList<OperationsWarehouse> warehouses, IReadOnlyList<OperationsInspectionQueueItem> approvals, bool canApprove,
        IReadOnlyList<OperationsHeldParcel>? unclaimed = null, int unclaimedCount = 0, int unclaimedDays = 0)
    {
        return new ListOperationsHoldsResponse
        {
            Succeeded = true, Items = items, TotalCount = totalCount, Warehouses = warehouses, Approvals = approvals, CanApprove = canApprove,
            Unclaimed = unclaimed ?? Array.Empty<OperationsHeldParcel>(), UnclaimedCount = unclaimedCount, UnclaimedDays = unclaimedDays
        };
    }

    public static ListOperationsHoldsResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new ListOperationsHoldsResponse { Error = error };
    }
}
