using Vixly.Platform.Application.UseCases.Payments;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Platform.Application.UseCases.Payments.GetPlatformInvoiceCardPayments;

// =========================================================
// FILE: GetPlatformInvoiceCardPaymentsResponse.cs
//
// PURPOSE:
// Reads the card side of an invoice: transactions, the card that would be charged and what can be refunded.
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class GetPlatformInvoiceCardPaymentsResponse
{
    private GetPlatformInvoiceCardPaymentsResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public PlatformInvoiceCardPaymentsItem? Payments { get; private init; }

    public static GetPlatformInvoiceCardPaymentsResponse Success(PlatformInvoiceCardPaymentsItem? payments)
    {
        return new GetPlatformInvoiceCardPaymentsResponse { Succeeded = true, Payments = payments };
    }

    public static GetPlatformInvoiceCardPaymentsResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new GetPlatformInvoiceCardPaymentsResponse { Error = error };
    }
}
