using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Billing.PayMyOperationsInvoiceByCard;

// =========================================================
// FILE: PayMyOperationsInvoiceByCardResponse.cs
//
// PURPOSE:
// A customer pays one of their invoices by card on the portal, on Stripe's payment page (Slice 5F step 4).
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class PayMyOperationsInvoiceByCardResponse
{
    private PayMyOperationsInvoiceByCardResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public Guid ChargeId { get; private init; }

    public string PageUrl { get; private init; } = string.Empty;

    public static PayMyOperationsInvoiceByCardResponse Success(Guid chargeId, string pageUrl)
    {
        return new PayMyOperationsInvoiceByCardResponse { Succeeded = true, ChargeId = chargeId, PageUrl = pageUrl };
    }

    public static PayMyOperationsInvoiceByCardResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new PayMyOperationsInvoiceByCardResponse { Error = error };
    }
}
