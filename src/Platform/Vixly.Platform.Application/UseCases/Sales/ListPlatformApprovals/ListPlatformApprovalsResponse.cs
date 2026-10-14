using Vixly.Platform.Application.Queries;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Platform.Application.UseCases.Sales.ListPlatformApprovals;

// =========================================================
// FILE: ListPlatformApprovalsResponse.cs
//
// PURPOSE:
// Lists what waits for the current staff member's approval (D-S24, D-S26).
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class ListPlatformApprovalsResponse
{
    private ListPlatformApprovalsResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public IReadOnlyList<PlatformApprovalItem> Items { get; private init; } = Array.Empty<PlatformApprovalItem>();

    public static ListPlatformApprovalsResponse Success(IReadOnlyList<PlatformApprovalItem> items)
    {
        return new ListPlatformApprovalsResponse { Succeeded = true, Items = items };
    }

    public static ListPlatformApprovalsResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new ListPlatformApprovalsResponse { Error = error };
    }
}
