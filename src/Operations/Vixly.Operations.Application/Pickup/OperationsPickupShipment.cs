using Vixly.Operations.Domain.Billing;
using Vixly.Operations.Domain.Pickup;
using Vixly.Operations.Domain.Shipments;
using Vixly.Operations.Domain.Tracking;

namespace Vixly.Operations.Application.Pickup;

// =========================================================
// FILE: OperationsPickupShipment.cs
//
// PURPOSE:
// A shipment at the pickup counter of a warehouse (Slice 7 step 2).
//
// RESPONSIBILITIES:
// - The shipment, its parcels (tracked), those ready here, its invoices and releases
//
// IMPORTANT NOTES:
// - Built by the pickup desk only
// =========================================================
public sealed record OperationsPickupShipment(OperationsShipment Shipment, IReadOnlyList<OperationsParcel> Parcels, IReadOnlyList<OperationsParcel> Ready,
    IReadOnlyList<OperationsInvoice> Invoices, IReadOnlyList<OperationsPickupRelease> Releases)
{
    // The last destination invoice (a shipment may be billed again at a later visit; review).
    public OperationsInvoice? DestinationInvoice => Invoices.Where(invoice => invoice.Kind == OperationsInvoiceKind.Destination).MaxBy(invoice => invoice.CreatedUtc);

    // Storage owed at the destination now and not billed yet, for the parcels still ready here (R-10).
    public decimal StorageOwed(DateTimeOffset utcNow) => Ready.Sum(parcel => parcel.UnbilledStorage(utcNow));
}
