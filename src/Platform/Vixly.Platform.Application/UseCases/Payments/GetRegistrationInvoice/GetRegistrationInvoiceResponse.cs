using Vixly.Platform.Application.UseCases.Payments;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Platform.Application.UseCases.Payments.GetRegistrationInvoice;

// =========================================================
// FILE: GetRegistrationInvoiceResponse.cs
//
// PURPOSE:
// Shows a self-registering company the invoice it must pay (D-P7).
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class GetRegistrationInvoiceResponse
{
    private GetRegistrationInvoiceResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public PlatformPaymentLinkSummary? Invoice { get; private init; }

    public static GetRegistrationInvoiceResponse Success(PlatformPaymentLinkSummary? invoice)
    {
        return new GetRegistrationInvoiceResponse { Succeeded = true, Invoice = invoice };
    }

    public static GetRegistrationInvoiceResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new GetRegistrationInvoiceResponse { Error = error };
    }
}
