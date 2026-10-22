using Vixly.Operations.Application.Arrivals;
using Vixly.Operations.Application.Pickup;
using Vixly.Operations.Domain.Pickup;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Pickup.GetOperationsPickup;

// =========================================================
// FILE: GetOperationsPickupResponse.cs
//
// PURPOSE:
// A shipment at the pickup counter: who may collect, what is ready, what is owed, the proof asked (Slice 7 step 2, R-4 to R-6, R-9, R-10).
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class GetOperationsPickupResponse
{
    private GetOperationsPickupResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public Guid ShipmentId { get; private init; }

    public string Number { get; private init; } = string.Empty;

    public string CustomerName { get; private init; } = string.Empty;

    public string? CustomerPhone { get; private init; }

    public string? RecipientName { get; private init; }

    public string? RecipientPhone { get; private init; }

    public string? CollectorName { get; private init; }

    public string? CollectorPhone { get; private init; }

    public OperationsArrivalPlace Here { get; private init; } = new(Guid.Empty, string.Empty, string.Empty, false);

    public IReadOnlyList<OperationsPickupParcelRow> Parcels { get; private init; } = Array.Empty<OperationsPickupParcelRow>();

    public IReadOnlyList<OperationsPickupInvoiceRow> Invoices { get; private init; } = Array.Empty<OperationsPickupInvoiceRow>();

    public IReadOnlyList<OperationsPickupReleaseRow> Releases { get; private init; } = Array.Empty<OperationsPickupReleaseRow>();

    public decimal StorageOwed { get; private init; }

    public string CurrencyCode { get; private init; } = string.Empty;

    public OperationsPickupProofs Proofs { get; private init; }

    public bool DestinationCharges { get; private init; }

    public bool CanBill { get; private init; }

    public bool CodeSent { get; private init; }

    public bool PayFirst { get; private init; }

    // Slice 7 step 4 (R-8): false for a partner agent whose partner does not handle money: no amounts, invoices or billing shown.
    public bool MoneyHandled { get; private init; }

    public static GetOperationsPickupResponse Success(Guid shipmentId, string number, string customerName, string? customerPhone, string? recipientName, string? recipientPhone, string? collectorName, string? collectorPhone, OperationsArrivalPlace here, IReadOnlyList<OperationsPickupParcelRow> parcels, IReadOnlyList<OperationsPickupInvoiceRow> invoices, IReadOnlyList<OperationsPickupReleaseRow> releases, decimal storageOwed, string currencyCode, OperationsPickupProofs proofs, bool destinationCharges, bool canBill, bool codeSent, bool payFirst, bool moneyHandled)
    {
        return new GetOperationsPickupResponse { Succeeded = true, ShipmentId = shipmentId, Number = number, CustomerName = customerName, CustomerPhone = customerPhone, RecipientName = recipientName, RecipientPhone = recipientPhone, CollectorName = collectorName, CollectorPhone = collectorPhone, Here = here, Parcels = parcels, Invoices = invoices, Releases = releases, StorageOwed = storageOwed, CurrencyCode = currencyCode, Proofs = proofs, DestinationCharges = destinationCharges, CanBill = canBill, CodeSent = codeSent, PayFirst = payFirst, MoneyHandled = moneyHandled };
    }

    public static GetOperationsPickupResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new GetOperationsPickupResponse { Error = error };
    }
}
