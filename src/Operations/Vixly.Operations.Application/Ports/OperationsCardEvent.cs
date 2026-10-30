namespace Vixly.Operations.Application.Ports;

// =========================================================
// FILE: OperationsCardEvent.cs
//
// PURPOSE:
// An event Stripe sent to the company's webhook, signature checked (Slice 5F step 4).
//
// RESPONSIBILITIES:
// - Carry the kind, our charge id from the metadata, and the payment state
//
// IMPORTANT NOTES:
// - Only the kinds Vixly acts on are read; others come back with Paid false
// =========================================================
public sealed record OperationsCardEvent(string Type, Guid? ChargeId, OperationsCardState State);
