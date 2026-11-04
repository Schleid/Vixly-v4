using Vixly.Operations.Application.Arrivals;
using Vixly.Operations.Domain.Dispatch;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Arrivals.GetOperationsArrival;

// =========================================================
// FILE: GetOperationsArrivalResponse.cs
//
// PURPOSE:
// One dispatch received at a destination warehouse: its bundles and what did not match (Slice 7, R-1 and R-2).
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class GetOperationsArrivalResponse
{
    private GetOperationsArrivalResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public Guid DispatchId { get; private init; }

    public string Number { get; private init; } = string.Empty;

    public string FromCode { get; private init; } = string.Empty;

    public OperationsDispatchStatus Status { get; private init; }

    public bool Onward { get; private init; }

    public DateTimeOffset? ReceivedUtc { get; private init; }

    public OperationsArrivalPlace Here { get; private init; } = new(Guid.Empty, string.Empty, string.Empty, false);

    public bool CanCustoms { get; private init; }

    public bool CanReceive { get; private init; }

    public IReadOnlyList<OperationsArrivalBundleRow> Bundles { get; private init; } = Array.Empty<OperationsArrivalBundleRow>();

    public IReadOnlyList<OperationsDiscrepancyRow> Discrepancies { get; private init; } = Array.Empty<OperationsDiscrepancyRow>();

    public static GetOperationsArrivalResponse Success(Guid dispatchId, string number, string fromCode, OperationsDispatchStatus status, bool onward, DateTimeOffset? receivedUtc, OperationsArrivalPlace here, bool canCustoms, bool canReceive, IReadOnlyList<OperationsArrivalBundleRow> bundles, IReadOnlyList<OperationsDiscrepancyRow> discrepancies)
    {
        return new GetOperationsArrivalResponse { Succeeded = true, DispatchId = dispatchId, Number = number, FromCode = fromCode, Status = status, Onward = onward, ReceivedUtc = receivedUtc, Here = here, CanCustoms = canCustoms, CanReceive = canReceive, Bundles = bundles, Discrepancies = discrepancies };
    }

    public static GetOperationsArrivalResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new GetOperationsArrivalResponse { Error = error };
    }
}
