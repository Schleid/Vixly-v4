using Vixly.Operations.Domain.Billing;
using Vixly.Operations.Domain.Locations;
using OperationsInvoice = Vixly.Operations.Domain.Billing.OperationsInvoice;

namespace Vixly.Operations.Application.UseCases.Cashier.ListOperationsPaymentsToVerify;

// =========================================================
// FILE: ListOperationsPaymentsToVerifyRequest.cs
//
// PURPOSE:
// Zelle and bank transfers waiting for finance (Slice 5F, C-6): the invoice, customer, amount, date and confirmation number.
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class ListOperationsPaymentsToVerifyRequest
{

}
