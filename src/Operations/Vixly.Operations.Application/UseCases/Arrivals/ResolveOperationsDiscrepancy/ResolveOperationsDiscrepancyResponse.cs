using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Arrivals.ResolveOperationsDiscrepancy;

// =========================================================
// FILE: ResolveOperationsDiscrepancyResponse.cs
//
// PURPOSE:
// Resolve a discrepancy: lost (the customer is told) or settled, with a note (Slice 7, R-2).
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class ResolveOperationsDiscrepancyResponse
{
    private ResolveOperationsDiscrepancyResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public static ResolveOperationsDiscrepancyResponse Success()
    {
        return new ResolveOperationsDiscrepancyResponse { Succeeded = true };
    }

    public static ResolveOperationsDiscrepancyResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new ResolveOperationsDiscrepancyResponse { Error = error };
    }
}
