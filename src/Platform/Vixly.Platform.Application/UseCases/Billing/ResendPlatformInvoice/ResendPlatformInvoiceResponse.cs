using Vixly.Shared.Kernel.Errors;

namespace Vixly.Platform.Application.UseCases.Billing.ResendPlatformInvoice;

// =========================================================
// FILE: ResendPlatformInvoiceResponse.cs
//
// PURPOSE:
// Emails an invoice again to the tenant's billing contact (D-B13).
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class ResendPlatformInvoiceResponse
{
    private ResendPlatformInvoiceResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public static ResendPlatformInvoiceResponse Success()
    {
        return new ResendPlatformInvoiceResponse { Succeeded = true };
    }

    public static ResendPlatformInvoiceResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new ResendPlatformInvoiceResponse { Error = error };
    }
}
