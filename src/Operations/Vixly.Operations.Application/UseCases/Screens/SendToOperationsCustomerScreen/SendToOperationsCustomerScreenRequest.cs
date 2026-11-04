namespace Vixly.Operations.Application.UseCases.Screens.SendToOperationsCustomerScreen;

// =========================================================
// FILE: SendToOperationsCustomerScreenRequest.cs
//
// PURPOSE:
// Sends an invoice to pay or a shipment to sign to a customer screen, or clears it for the next customer (Slice 5F step 3, P-2, P-3).
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class SendToOperationsCustomerScreenRequest
{
    public Guid ScreenId { get; init; }

    public Guid? InvoiceId { get; init; }

    public Guid? ShipmentId { get; init; }

    public bool Clear { get; init; }
}
