using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Arrivals.RecordOperationsCustoms;

// =========================================================
// FILE: RecordOperationsCustomsResponse.cs
//
// PURPOSE:
// Record customs for a dispatch or one of its bundles at a warehouse that clears customs (Slice 7, R-3).
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class RecordOperationsCustomsResponse
{
    private RecordOperationsCustomsResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public int Reached { get; private init; }

    public int Ready { get; private init; }

    public static RecordOperationsCustomsResponse Success(int reached, int ready)
    {
        return new RecordOperationsCustomsResponse { Succeeded = true, Reached = reached, Ready = ready };
    }

    public static RecordOperationsCustomsResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new RecordOperationsCustomsResponse { Error = error };
    }
}
