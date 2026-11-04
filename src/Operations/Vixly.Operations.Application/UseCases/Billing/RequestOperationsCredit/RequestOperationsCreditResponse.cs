using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Billing.RequestOperationsCredit;

// =========================================================
// FILE: RequestOperationsCreditResponse.cs
//
// PURPOSE:
// Ask for a credit note or a void of an invoice, with a reason (Slice 5F, F-6 and I-6).
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class RequestOperationsCreditResponse
{
    private RequestOperationsCreditResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public Guid CreditNoteId { get; private init; }

    public static RequestOperationsCreditResponse Success(Guid creditNoteId)
    {
        return new RequestOperationsCreditResponse { Succeeded = true, CreditNoteId = creditNoteId };
    }

    public static RequestOperationsCreditResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new RequestOperationsCreditResponse { Error = error };
    }
}
