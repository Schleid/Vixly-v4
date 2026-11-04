using Vixly.Operations.Domain.Billing;
using Vixly.Operations.Domain.Locations;
using OperationsInvoice = Vixly.Operations.Domain.Billing.OperationsInvoice;

namespace Vixly.Operations.Application.Billing;

// =========================================================
// FILE: OperationsBillingRows.cs
//
// PURPOSE:
// Turns invoices into list rows (Slice 5F, I-8).
//
// RESPONSIBILITIES:
// - Name the customer and the warehouse of each
//
// IMPORTANT NOTES:
// - A pure function
// =========================================================
public static class OperationsBillingRows
{
    public static string Customer(OperationsInvoice invoice) =>
        (invoice ?? throw new ArgumentNullException(nameof(invoice))).Party.CustomerName + (invoice.Party.CustomerCode is { } code ? " · " + code : string.Empty);

    public static OperationsInvoiceRow Row(OperationsInvoice invoice, IReadOnlyDictionary<Guid, OperationsWarehouse> warehouses)
    {
        ArgumentNullException.ThrowIfNull(invoice);
        ArgumentNullException.ThrowIfNull(warehouses);

        return new OperationsInvoiceRow(invoice.Id, invoice.Number, invoice.Kind, invoice.Reference, Customer(invoice), invoice.IssuedOn, invoice.CurrencyCode, invoice.Total, invoice.CreditedTotal,
            invoice.Owed, invoice.Status == OperationsInvoiceStatus.Voided, warehouses.TryGetValue(invoice.WarehouseId, out OperationsWarehouse? warehouse) ? warehouse.Code : string.Empty,
            invoice.PendingTotal);
    }
}
