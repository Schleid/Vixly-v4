using Vixly.Platform.Application.Queries;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Platform.Application.UseCases.Tenants.ListAccountManagerCandidates;

// =========================================================
// FILE: ListAccountManagerCandidatesResponse.cs
//
// PURPOSE:
// Result of ListAccountManagerCandidates.
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class ListAccountManagerCandidatesResponse
{
    private ListAccountManagerCandidatesResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public IReadOnlyList<PlatformStaffOptionItem> Staff { get; private init; } = Array.Empty<PlatformStaffOptionItem>();

    public static ListAccountManagerCandidatesResponse Success(IReadOnlyList<PlatformStaffOptionItem> staff)
    {
        return new ListAccountManagerCandidatesResponse { Succeeded = true, Staff = staff };
    }

    public static ListAccountManagerCandidatesResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new ListAccountManagerCandidatesResponse { Error = error };
    }
}
