using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Receiving.CloseOperationsDelivery;

// =========================================================
// FILE: CloseOperationsDeliveryResponse.cs
//
// PURPOSE:
// Closes a carrier delivery and issues its warehouse receipt (Slice 3B, owner 2026-10-18).
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class CloseOperationsDeliveryResponse
{
    private CloseOperationsDeliveryResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public Guid ReceiptId { get; private init; }

    public string Number { get; private init; } = string.Empty;

    public static CloseOperationsDeliveryResponse Success(Guid receiptId, string number)
    {
        return new CloseOperationsDeliveryResponse { Succeeded = true, ReceiptId = receiptId, Number = number };
    }

    public static CloseOperationsDeliveryResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new CloseOperationsDeliveryResponse { Error = error };
    }
}
