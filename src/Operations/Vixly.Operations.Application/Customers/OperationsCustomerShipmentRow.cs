namespace Vixly.Operations.Application.Customers;

// =========================================================
// FILE: OperationsCustomerShipmentRow.cs
//
// PURPOSE:
// One of the customer's shipments on the customer page (owner, 2026-10-31: the page showed only what is coming in).
//
// RESPONSIBILITIES:
// - Carry the number, when it was created, the service, the route, the parcel count and where it stands
//
// IMPORTANT NOTES:
// - State: waiting, open, booked, picked_up or delivered (texts ui.customers.shipment_state.*)
// =========================================================
public sealed record OperationsCustomerShipmentRow(Guid Id, string Number, DateTimeOffset CreatedUtc, string? ServiceCode, string Origin, string? Destination, int? Parcels, string State);
