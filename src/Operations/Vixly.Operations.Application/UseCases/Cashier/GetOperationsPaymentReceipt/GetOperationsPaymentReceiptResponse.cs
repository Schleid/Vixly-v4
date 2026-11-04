using Vixly.Operations.Domain.Billing;
using Vixly.Operations.Domain.Locations;
using OperationsInvoice = Vixly.Operations.Domain.Billing.OperationsInvoice;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Cashier.GetOperationsPaymentReceipt;

// =========================================================
// FILE: GetOperationsPaymentReceiptResponse.cs
//
// PURPOSE:
// The receipt of a payment as a PDF, in the customer's language (Slice 5F, C-3).
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class GetOperationsPaymentReceiptResponse
{
    private GetOperationsPaymentReceiptResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public byte[] Content { get; private init; } = Array.Empty<byte>();

    public string FileName { get; private init; } = string.Empty;

    public static GetOperationsPaymentReceiptResponse Success(byte[] content, string fileName)
    {
        return new GetOperationsPaymentReceiptResponse { Succeeded = true, Content = content, FileName = fileName };
    }

    public static GetOperationsPaymentReceiptResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new GetOperationsPaymentReceiptResponse { Error = error };
    }
}
