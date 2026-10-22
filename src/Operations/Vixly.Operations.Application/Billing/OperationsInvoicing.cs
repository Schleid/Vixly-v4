using Vixly.Operations.Application.Charges;
using Vixly.Operations.Application.Ports;
using Vixly.Operations.Application.Settings;
using Vixly.Operations.Domain.Billing;
using OperationsInvoice = Vixly.Operations.Domain.Billing.OperationsInvoice;
using Vixly.Operations.Domain.Charges;
using Vixly.Operations.Domain.Customers;
using Vixly.Operations.Domain.Locations;
using Vixly.Operations.Domain.Settings;
using Vixly.Operations.Domain.Shipments;
using Vixly.Operations.Domain.Supplies;

namespace Vixly.Operations.Application.Billing;

// =========================================================
// FILE: OperationsInvoicing.cs
//
// PURPOSE:
// Issues invoices by themselves (Slice 5F, F-1 and I-1 to I-4): a shipment's when it becomes final, an order's when the clerk confirms it.
//
// RESPONSIBILITIES:
// - Build the lines: the freight, the charges and extras, the insurance, the supplies with their discount
// - Keep the parties, the route and the company's tax settings of the day
// - Number and add the invoice
//
// IMPORTANT NOTES:
// - The caller saves; nothing here saves
// - Never twice: a shipment or order that already has its invoice is left alone
// - In the customer's language: the supplies' names are taken in it
// =========================================================
public sealed class OperationsInvoicing
{
    private readonly IOperationsBillingRepository _billing;

    private readonly IOperationsSettingsRepository _settings;

    private readonly IOperationsCustomerRepository _customers;

    private readonly IOperationsLocationRepository _locations;

    private readonly IOperationsBrandRepository _brands;

    private readonly IOperationsSupplyRepository _supplies;

    private readonly OperationsNumbering _numbering;

    public OperationsInvoicing(IOperationsBillingRepository billing, IOperationsSettingsRepository settings, IOperationsCustomerRepository customers, IOperationsLocationRepository locations,
        IOperationsBrandRepository brands, IOperationsSupplyRepository supplies, OperationsNumbering numbering)
    {
        _billing = billing ?? throw new ArgumentNullException(nameof(billing));
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        _customers = customers ?? throw new ArgumentNullException(nameof(customers));
        _locations = locations ?? throw new ArgumentNullException(nameof(locations));
        _brands = brands ?? throw new ArgumentNullException(nameof(brands));
        _supplies = supplies ?? throw new ArgumentNullException(nameof(supplies));
        _numbering = numbering ?? throw new ArgumentNullException(nameof(numbering));
    }

    // The tax kind of a shipment's charge line (I-3 and I-4).
    public static OperationsTaxKind TaxKind(OperationsChargeLineKind kind) => kind switch
    {
        OperationsChargeLineKind.Insurance => OperationsTaxKind.Insurance,
        OperationsChargeLineKind.ServiceFee or OperationsChargeLineKind.Consolidation or OperationsChargeLineKind.Extra => OperationsTaxKind.Charges,
        _ => OperationsTaxKind.Freight
    };

    // A shipment just became final: its invoice with the lines it was priced with, and its supplies.
    public async Task<OperationsInvoice?> IssueForShipmentAsync(OperationsShipment shipment, OperationsRating rating, string currencyCode, OperationsSupplySale? supplies,
        DateTimeOffset utcNow, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(shipment);
        ArgumentNullException.ThrowIfNull(rating);

        if (await _billing.FindShipmentInvoiceAsync(shipment.Id, cancellationToken) is not null || await _customers.GetAsync(shipment.CustomerId, cancellationToken) is not { } customer)
        {
            return null;
        }

        // The invoice carries the shipment's number: one without it gets it now, as its other documents.
        if (shipment.Number is null)
        {
            shipment.Numbered(await _numbering.NextAsync(OperationsNumberKind.Shipment, cancellationToken));
        }

        string language = customer.PreferredLanguage.Value;
        List<OperationsInvoiceLine> lines = rating.Lines.Select(line => new OperationsInvoiceLine(line.Kind, null, null, line.Label, line.Piece, line.Quantity, line.Basis, line.UnitPrice,
            line.Amount, 0m, 0m, TaxKind(line.Kind), !line.IsGovernmentFee)).ToList();
        lines.AddRange(await SupplyLinesAsync(supplies, language, cancellationToken));
        IReadOnlyList<OperationsWarehouse> warehouses = await _locations.ListWarehousesAsync(cancellationToken);
        OperationsWarehouse? origin = warehouses.FirstOrDefault(warehouse => warehouse.Id == shipment.OriginWarehouseId);
        string route = (origin?.Name ?? string.Empty) + " → "
            + (shipment.DestinationWarehouseId is { } to && warehouses.FirstOrDefault(warehouse => warehouse.Id == to) is { } destination ? destination.Name : shipment.DestinationCountryCode ?? string.Empty);
        return await IssueAsync(OperationsInvoiceKind.Shipment, customer, origin, shipment.Id, supplies?.Id, shipment.Number ?? string.Empty, route, shipment.ServiceCode, currencyCode, lines,
            utcNow, cancellationToken, shipment.FreightCollect);
    }

    // Slice 7 step 2 (R-4, R-10): a shipment's destination charges and storage, billed at the pickup counter of the warehouse; in the shipment invoice's currency.
    public async Task<OperationsInvoice?> IssueForDestinationAsync(OperationsShipment shipment, OperationsWarehouse warehouse, IReadOnlyList<OperationsInvoiceLine> lines, DateTimeOffset utcNow,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(shipment);
        ArgumentNullException.ThrowIfNull(warehouse);
        ArgumentNullException.ThrowIfNull(lines);

        if (await _customers.GetAsync(shipment.CustomerId, cancellationToken) is not { } customer)
        {
            return null;
        }

        string currency = (await _billing.FindShipmentInvoiceAsync(shipment.Id, cancellationToken))?.CurrencyCode
            ?? (await _settings.FindAsync(cancellationToken))?.CurrencyCode ?? OperationsTenantSettings.DefaultCurrency;
        OperationsWarehouse? origin = await _locations.GetWarehouseAsync(shipment.OriginWarehouseId, cancellationToken);
        return await IssueAsync(OperationsInvoiceKind.Destination, customer, warehouse, shipment.Id, null, shipment.Number ?? string.Empty, (origin?.Name ?? string.Empty) + " → " + warehouse.Name,
            shipment.ServiceCode, currency, lines, utcNow, cancellationToken);
    }

    // An order of supplies on its own was confirmed: its own invoice (F-1).
    public async Task<OperationsInvoice?> IssueForOrderAsync(OperationsSupplySale sale, DateTimeOffset utcNow, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(sale);

        if (sale.ShipmentId is not null || await _billing.FindOrderInvoiceAsync(sale.Id, cancellationToken) is not null
            || await _customers.GetAsync(sale.CustomerId, cancellationToken) is not { } customer)
        {
            return null;
        }

        IReadOnlyList<OperationsInvoiceLine> lines = await SupplyLinesAsync(sale, customer.PreferredLanguage.Value, cancellationToken);
        OperationsWarehouse? counter = await _locations.GetWarehouseAsync(sale.WarehouseId, cancellationToken);
        return await IssueAsync(OperationsInvoiceKind.SupplyOrder, customer, counter, null, sale.Id, sale.Number ?? string.Empty, null, null, sale.CurrencyCode, lines, utcNow,
            cancellationToken);
    }

    private async Task<IReadOnlyList<OperationsInvoiceLine>> SupplyLinesAsync(OperationsSupplySale? sale, string language, CancellationToken cancellationToken)
    {
        if (sale is not { Status: OperationsSupplySaleStatus.Confirmed } || sale.Lines.Count == 0)
        {
            return Array.Empty<OperationsInvoiceLine>();
        }

        Dictionary<Guid, OperationsSupply> catalogue = (await _supplies.ListAsync(cancellationToken)).ToDictionary(supply => supply.Id);
        return sale.Lines.Select(line => new OperationsInvoiceLine(null, line.SupplyId, line.Code, catalogue.TryGetValue(line.SupplyId, out OperationsSupply? supply) ? supply.Name(language) : line.Name,
            null, line.Quantity, null, line.UnitPrice, line.Gross, line.DiscountPercent, line.Discount, OperationsTaxKind.Supplies, line.Taxed)).ToList();
    }

    private async Task<OperationsInvoice?> IssueAsync(OperationsInvoiceKind kind, OperationsCustomer customer, OperationsWarehouse? warehouse, Guid? shipmentId, Guid? saleId, string reference,
        string? route, string? serviceCode, string currencyCode, IReadOnlyList<OperationsInvoiceLine> lines, DateTimeOffset utcNow, CancellationToken cancellationToken, bool payAtDestination = false)
    {
        if (lines.Count == 0 || warehouse is null)
        {
            return null;
        }

        OperationsTenantSettings settings = await _settings.FindAsync(cancellationToken) ?? OperationsTenantSettings.CreateDefault(customer.TenantId, utcNow);
        string language = customer.PreferredLanguage.Value;
        OperationsInvoiceParty party = new((await _brands.FindAsync(cancellationToken))?.DisplayName ?? string.Empty, settings.TaxNumber, warehouse.Name, customer.Code,
            customer.FirstName + " " + customer.LastName, customer.CompanyName, customer.Phone, customer.Email.Value);
        OperationsInvoiceDraft draft = new(kind, customer.Id, warehouse.Id, shipmentId, saleId, reference, route, serviceCode, currencyCode, language, party, lines, settings.TaxName,
            settings.TaxPerKind, settings.TaxRates, settings.InvoiceNote(language)) { PayAtDestination = payAtDestination };
        OperationsInvoice invoice = OperationsInvoice.Issue(customer.TenantId, await _numbering.NextAsync(OperationsNumberKind.Invoice, cancellationToken), draft,
            OperationsTimeZones.Today(utcNow, settings.TimeZone), utcNow);
        _billing.Add(invoice);
        return invoice;
    }
}
