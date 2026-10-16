using Vixly.Platform.Domain.Payments;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Platform.Application.UseCases.Payments.ChargePlatformInvoiceCard;

// =========================================================
// FILE: ChargePlatformInvoiceCardResponse.cs
//
// PURPOSE:
// Charges the tenant's default saved card for an invoice's balance (D-P4).
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class ChargePlatformInvoiceCardResponse
{
    private ChargePlatformInvoiceCardResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public PlatformCardTransactionStatus Status { get; private init; }

    public string? FailureCode { get; private init; }

    public static ChargePlatformInvoiceCardResponse Success(PlatformCardTransactionStatus status, string? failureCode)
    {
        return new ChargePlatformInvoiceCardResponse { Succeeded = true, Status = status, FailureCode = failureCode };
    }

    public static ChargePlatformInvoiceCardResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new ChargePlatformInvoiceCardResponse { Error = error };
    }
}
