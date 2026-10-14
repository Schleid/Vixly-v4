using Vixly.Platform.Application.Queries;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Platform.Application.UseCases.Audit.GetAuditEntry;

// =========================================================
// FILE: GetAuditEntryResponse.cs
//
// PURPOSE:
// Result of GetAuditEntry.
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class GetAuditEntryResponse
{
    private GetAuditEntryResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public PlatformAuditEntryDetail? Entry { get; private init; }

    public static GetAuditEntryResponse Success(PlatformAuditEntryDetail? entry)
    {
        return new GetAuditEntryResponse { Succeeded = true, Entry = entry };
    }

    public static GetAuditEntryResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new GetAuditEntryResponse { Error = error };
    }
}
