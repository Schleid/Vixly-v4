using Vixly.Operations.Domain.Billing;

namespace Vixly.Operations.Application.Pickup;

// =========================================================
// FILE: OperationsPickupInvoiceRow.cs
//
// PURPOSE:
// An invoice of a shipment at the pickup counter (Slice 7 step 2).
//
// RESPONSIBILITIES:
// - Carry what the pages show
//
// IMPORTANT NOTES:
// - Read model only
// =========================================================
public sealed record OperationsPickupInvoiceRow(Guid InvoiceId, string Number, OperationsInvoiceKind Kind, decimal Total, decimal Owed, OperationsPaymentState State, string CurrencyCode);
