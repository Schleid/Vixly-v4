using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Billing.DecideOperationsCredit;

// =========================================================
// FILE: DecideOperationsCreditResponse.cs
//
// PURPOSE:
// Approve or refuse a credit note or a void (Slice 5F, F-6, I-6 and I-7).
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class DecideOperationsCreditResponse
{
    private DecideOperationsCreditResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public Guid InvoiceId { get; private init; }

    public string? Number { get; private init; }

    public static DecideOperationsCreditResponse Success(Guid invoiceId, string? number)
    {
        return new DecideOperationsCreditResponse { Succeeded = true, InvoiceId = invoiceId, Number = number };
    }

    public static DecideOperationsCreditResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new DecideOperationsCreditResponse { Error = error };
    }
}
