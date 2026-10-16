using Vixly.Shared.Kernel.Errors;

namespace Vixly.Platform.Application.UseCases.Billing.BillPlatformChargesNow;

// =========================================================
// FILE: BillPlatformChargesNowResponse.cs
//
// PURPOSE:
// Invoices a subscription's pending one-time charges now instead of with the next invoice (D-B5).
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class BillPlatformChargesNowResponse
{
    private BillPlatformChargesNowResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public Guid PlatformInvoiceId { get; private init; }

    public static BillPlatformChargesNowResponse Success(Guid platformInvoiceId)
    {
        return new BillPlatformChargesNowResponse { Succeeded = true, PlatformInvoiceId = platformInvoiceId };
    }

    public static BillPlatformChargesNowResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new BillPlatformChargesNowResponse { Error = error };
    }
}
