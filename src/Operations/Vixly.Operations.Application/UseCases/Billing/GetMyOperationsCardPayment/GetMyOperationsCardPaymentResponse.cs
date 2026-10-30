using Vixly.Operations.Domain.Billing;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Billing.GetMyOperationsCardPayment;

// =========================================================
// FILE: GetMyOperationsCardPaymentResponse.cs
//
// PURPOSE:
// A customer's card payment as it now stands, when they come back from Stripe's page (Slice 5F step 4).
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class GetMyOperationsCardPaymentResponse
{
    private GetMyOperationsCardPaymentResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public Guid InvoiceId { get; private init; }

    public string InvoiceNumber { get; private init; } = string.Empty;

    public OperationsCardChargeStatus Status { get; private init; }

    public decimal Amount { get; private init; }

    public string CurrencyCode { get; private init; } = string.Empty;

    // Stripe's page while it waits; none once paid or over.
    public string? PageUrl { get; private init; }

    public static GetMyOperationsCardPaymentResponse Success(Guid invoiceId, string invoiceNumber, OperationsCardChargeStatus status, decimal amount, string currencyCode, string? pageUrl)
    {
        return new GetMyOperationsCardPaymentResponse { Succeeded = true, InvoiceId = invoiceId, InvoiceNumber = invoiceNumber, Status = status, Amount = amount, CurrencyCode = currencyCode, PageUrl = pageUrl };
    }

    public static GetMyOperationsCardPaymentResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new GetMyOperationsCardPaymentResponse { Error = error };
    }
}
