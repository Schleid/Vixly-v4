using Vixly.Operations.Domain.Billing;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Cashier.GetOperationsCardPayment;

// =========================================================
// FILE: GetOperationsCardPaymentResponse.cs
//
// PURPOSE:
// A card payment at the cashier as it now stands, asking Stripe while it waits (Slice 5F step 4).
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class GetOperationsCardPaymentResponse
{
    private GetOperationsCardPaymentResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public Guid ChargeId { get; private init; }

    public Guid InvoiceId { get; private init; }

    public string InvoiceNumber { get; private init; } = string.Empty;

    public OperationsCardChannel Channel { get; private init; }

    public OperationsCardChargeStatus Status { get; private init; }

    public decimal Amount { get; private init; }

    public string CurrencyCode { get; private init; } = string.Empty;

    public string? PageUrl { get; private init; }

    public string? ReaderLabel { get; private init; }

    public string? PaymentNumber { get; private init; }

    public Guid? PaymentId { get; private init; }

    public static GetOperationsCardPaymentResponse Success(Guid chargeId, Guid invoiceId, string invoiceNumber, OperationsCardChannel channel, OperationsCardChargeStatus status, decimal amount, string currencyCode, string? pageUrl, string? readerLabel, string? paymentNumber, Guid? paymentId)
    {
        return new GetOperationsCardPaymentResponse { Succeeded = true, ChargeId = chargeId, InvoiceId = invoiceId, InvoiceNumber = invoiceNumber, Channel = channel, Status = status, Amount = amount, CurrencyCode = currencyCode, PageUrl = pageUrl, ReaderLabel = readerLabel, PaymentNumber = paymentNumber, PaymentId = paymentId };
    }

    public static GetOperationsCardPaymentResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new GetOperationsCardPaymentResponse { Error = error };
    }
}
