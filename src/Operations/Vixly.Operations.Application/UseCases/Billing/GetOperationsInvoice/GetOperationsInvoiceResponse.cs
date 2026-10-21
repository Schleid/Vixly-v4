using Vixly.Operations.Domain.Billing;
using OperationsInvoice = Vixly.Operations.Domain.Billing.OperationsInvoice;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Billing.GetOperationsInvoice;

// =========================================================
// FILE: GetOperationsInvoiceResponse.cs
//
// PURPOSE:
// One invoice with its lines, taxes and credit notes (Slice 5F, I-2, I-6 and I-8).
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class GetOperationsInvoiceResponse
{
    private GetOperationsInvoiceResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public OperationsInvoice Invoice { get; private init; } = null!;

    public IReadOnlyList<OperationsCreditNote> CreditNotes { get; private init; } = Array.Empty<OperationsCreditNote>();

    public IReadOnlyDictionary<Guid, string> People { get; private init; } = new Dictionary<Guid, string>();

    public bool CanRequest { get; private init; }

    public bool CanApprove { get; private init; }

    public Guid UserId { get; private init; }

    public static GetOperationsInvoiceResponse Success(OperationsInvoice invoice, IReadOnlyList<OperationsCreditNote> creditNotes, IReadOnlyDictionary<Guid, string> people, bool canRequest, bool canApprove, Guid userId)
    {
        return new GetOperationsInvoiceResponse { Succeeded = true, Invoice = invoice, CreditNotes = creditNotes, People = people, CanRequest = canRequest, CanApprove = canApprove, UserId = userId };
    }

    public static GetOperationsInvoiceResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new GetOperationsInvoiceResponse { Error = error };
    }
}
