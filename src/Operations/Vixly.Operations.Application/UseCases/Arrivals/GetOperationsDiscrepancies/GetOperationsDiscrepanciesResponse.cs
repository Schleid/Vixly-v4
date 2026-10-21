using Vixly.Operations.Application.Arrivals;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Arrivals.GetOperationsDiscrepancies;

// =========================================================
// FILE: GetOperationsDiscrepanciesResponse.cs
//
// PURPOSE:
// What did not match at a destination, for the destination and the origin (Slice 7, R-2).
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class GetOperationsDiscrepanciesResponse
{
    private GetOperationsDiscrepanciesResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public IReadOnlyList<OperationsDiscrepancyRow> Items { get; private init; } = Array.Empty<OperationsDiscrepancyRow>();

    public static GetOperationsDiscrepanciesResponse Success(IReadOnlyList<OperationsDiscrepancyRow> items)
    {
        return new GetOperationsDiscrepanciesResponse { Succeeded = true, Items = items };
    }

    public static GetOperationsDiscrepanciesResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new GetOperationsDiscrepanciesResponse { Error = error };
    }
}
