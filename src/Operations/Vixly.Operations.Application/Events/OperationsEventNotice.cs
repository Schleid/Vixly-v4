using Vixly.Operations.Application.Ports;

namespace Vixly.Operations.Application.Events;

// =========================================================
// FILE: OperationsEventNotice.cs
//
// PURPOSE:
// Tell a customer about a delay or an exception on a shipment (Slice 6C, E-5).
//
// RESPONSIBILITIES:
// - Carry the customer, the portal, the shipment and the cause with its words
//
// IMPORTANT NOTES:
// - One per shipment per event, written to the outbox with the event
// =========================================================
public sealed record OperationsEventNotice(Guid CustomerId, string PortalSlug, string ShipmentNumber, bool Exception, string Cause, string? LabelEn, string? LabelFr, string? LabelEs, string? Message)
{
    public const string MessageType = "ops.events.update.v1";

    // A package not yet in a shipment: the number is the package's.
    public bool Package { get; init; }

    public static OperationsCustomerMailKind Kind => OperationsCustomerMailKind.ShipmentUpdate;
}
