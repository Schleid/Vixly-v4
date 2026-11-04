using Vixly.Operations.Domain.Billing;
using Vixly.Operations.Domain.Shipments;

namespace Vixly.Operations.Application.Screens;

// =========================================================
// FILE: OperationsScreenState.cs
//
// PURPOSE:
// A customer screen's invoice and card payment as they stand, and the version the page shows (Slice 5F step 3).
//
// RESPONSIBILITIES:
// - Carry them together
//
// IMPORTANT NOTES:
// - The version changes whenever the step, the invoice, the shipment to sign or the card payment changes: the screen then reloads,
//   and a choice made on the page before is refused (review: never a signature on a declaration that changed)
// =========================================================
public sealed record OperationsScreenState(OperationsInvoice? Invoice, OperationsCardCharge? Charge, OperationsShipment? Shipment, string Version);
