using Vixly.Operations.Application.Audit;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Audit.ListOperationsAuditEntries;

// =========================================================
// FILE: ListOperationsAuditEntriesResponse.cs
//
// PURPOSE:
// Lists the tenant's audit entries for the audit log screen (Slice 1E).
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class ListOperationsAuditEntriesResponse
{
    private ListOperationsAuditEntriesResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public IReadOnlyList<OperationsAuditLine> Lines { get; private init; } = Array.Empty<OperationsAuditLine>();

    public int TotalCount { get; private init; }

    public static ListOperationsAuditEntriesResponse Success(IReadOnlyList<OperationsAuditLine> lines, int totalCount)
    {
        return new ListOperationsAuditEntriesResponse { Succeeded = true, Lines = lines, TotalCount = totalCount };
    }

    public static ListOperationsAuditEntriesResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new ListOperationsAuditEntriesResponse { Error = error };
    }
}
