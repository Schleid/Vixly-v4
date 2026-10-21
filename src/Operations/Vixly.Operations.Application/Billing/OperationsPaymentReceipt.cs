using Vixly.Operations.Domain.Billing;
using OperationsInvoice = Vixly.Operations.Domain.Billing.OperationsInvoice;

namespace Vixly.Operations.Application.Billing;

// =========================================================
// FILE: OperationsPaymentReceipt.cs
//
// PURPOSE:
// What a payment receipt prints (Slice 5F, C-3).
//
// RESPONSIBILITIES:
// - Carry the payment, its invoice and the company's names of its other methods
//
// IMPORTANT NOTES:
// - Printed in the invoice's language
// =========================================================
public sealed record OperationsPaymentReceipt(OperationsPayment Payment, OperationsInvoice Invoice, IReadOnlyDictionary<OperationsPaymentMethod, string> OtherNames, DateOnly Day);
