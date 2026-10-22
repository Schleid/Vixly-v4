using Vixly.Operations.Application.Audit;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Audit.ListOperationsMemberActivity;

// =========================================================
// FILE: ListOperationsMemberActivityResponse.cs
//
// PURPOSE:
// Member activity (owner, 2026-10-21): the company's sign-ins and sign-outs, with the device, the browser and the country, newest first.
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class ListOperationsMemberActivityResponse
{
    private ListOperationsMemberActivityResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public IReadOnlyList<OperationsActivityLine> Lines { get; private init; } = Array.Empty<OperationsActivityLine>();

    public int TotalCount { get; private init; }

    public static ListOperationsMemberActivityResponse Success(IReadOnlyList<OperationsActivityLine> lines, int totalCount)
    {
        return new ListOperationsMemberActivityResponse { Succeeded = true, Lines = lines, TotalCount = totalCount };
    }

    public static ListOperationsMemberActivityResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new ListOperationsMemberActivityResponse { Error = error };
    }
}
