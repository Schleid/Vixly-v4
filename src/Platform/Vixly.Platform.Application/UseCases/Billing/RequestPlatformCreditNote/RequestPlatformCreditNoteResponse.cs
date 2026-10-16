using Vixly.Shared.Kernel.Errors;

namespace Vixly.Platform.Application.UseCases.Billing.RequestPlatformCreditNote;

// =========================================================
// FILE: RequestPlatformCreditNoteResponse.cs
//
// PURPOSE:
// Asks the sales manager to approve a credit note or a void (D-B6).
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class RequestPlatformCreditNoteResponse
{
    private RequestPlatformCreditNoteResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public static RequestPlatformCreditNoteResponse Success()
    {
        return new RequestPlatformCreditNoteResponse { Succeeded = true };
    }

    public static RequestPlatformCreditNoteResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new RequestPlatformCreditNoteResponse { Error = error };
    }
}
