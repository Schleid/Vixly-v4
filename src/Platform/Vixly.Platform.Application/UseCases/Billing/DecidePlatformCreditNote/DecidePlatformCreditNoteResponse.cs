using Vixly.Shared.Kernel.Errors;

namespace Vixly.Platform.Application.UseCases.Billing.DecidePlatformCreditNote;

// =========================================================
// FILE: DecidePlatformCreditNoteResponse.cs
//
// PURPOSE:
// The sales manager issues or rejects a requested credit note or void; never the requester (D-B6).
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class DecidePlatformCreditNoteResponse
{
    private DecidePlatformCreditNoteResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public static DecidePlatformCreditNoteResponse Success()
    {
        return new DecidePlatformCreditNoteResponse { Succeeded = true };
    }

    public static DecidePlatformCreditNoteResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new DecidePlatformCreditNoteResponse { Error = error };
    }
}
