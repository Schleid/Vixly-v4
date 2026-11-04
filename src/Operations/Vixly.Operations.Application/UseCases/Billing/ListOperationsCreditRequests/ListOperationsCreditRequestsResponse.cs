using Vixly.Operations.Application.Billing;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Billing.ListOperationsCreditRequests;

// =========================================================
// FILE: ListOperationsCreditRequestsResponse.cs
//
// PURPOSE:
// The credit notes and voids waiting for a decision in the warehouses a person approves for (Slice 5F, I-6).
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class ListOperationsCreditRequestsResponse
{
    private ListOperationsCreditRequestsResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public IReadOnlyList<OperationsCreditRequestRow> Requests { get; private init; } = Array.Empty<OperationsCreditRequestRow>();

    public static ListOperationsCreditRequestsResponse Success(IReadOnlyList<OperationsCreditRequestRow> requests)
    {
        return new ListOperationsCreditRequestsResponse { Succeeded = true, Requests = requests };
    }

    public static ListOperationsCreditRequestsResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new ListOperationsCreditRequestsResponse { Error = error };
    }
}
