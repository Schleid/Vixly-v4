using System.Globalization;
using Vixly.Operations.Application.Ports;
using Vixly.Operations.Domain.Charges;
using Vixly.Operations.Domain.Customers;
using Vixly.Operations.Domain.Locations;
using Vixly.Operations.Domain.Settings;

namespace Vixly.Operations.Application.Charges;

// =========================================================
// FILE: OperationsQuoteDocuments.cs
//
// PURPOSE:
// Draws a quote's PDF in its customer's language (owner, 2026-10-28).
//
// RESPONSIBILITIES:
// - Gather the customer, the route, the company's units and its dates in the company's time zone
// - Switch to the customer's language while the PDF is drawn
//
// IMPORTANT NOTES:
// - The caller checks who may see the quote
// =========================================================
public sealed class OperationsQuoteDocuments
{
    private readonly IOperationsCustomerRepository _customers;

    private readonly IOperationsLocationRepository _locations;

    private readonly IOperationsSettingsRepository _settings;

    private readonly IOperationsBrandRepository _brands;

    private readonly IOperationsLabelRenderer _renderer;

    public OperationsQuoteDocuments(IOperationsCustomerRepository customers, IOperationsLocationRepository locations, IOperationsSettingsRepository settings,
        IOperationsBrandRepository brands, IOperationsLabelRenderer renderer)
    {
        _customers = customers ?? throw new ArgumentNullException(nameof(customers));
        _locations = locations ?? throw new ArgumentNullException(nameof(locations));
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        _brands = brands ?? throw new ArgumentNullException(nameof(brands));
        _renderer = renderer ?? throw new ArgumentNullException(nameof(renderer));
    }

    // The PDF, the customer it is for and the company's name; null when the customer is gone.
    public async Task<(byte[] Pdf, OperationsCustomer Customer, string Company, DateOnly ValidUntil)?> RenderAsync(OperationsQuote quote, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(quote);

        if (await _customers.GetAsync(quote.CustomerId, cancellationToken) is not { } customer)
        {
            return null;
        }

        OperationsTenantSettings? settings = await _settings.FindAsync(cancellationToken);
        Dictionary<Guid, OperationsWarehouse> warehouses = (await _locations.ListWarehousesAsync(cancellationToken)).ToDictionary(item => item.Id);
        string company = (await _brands.FindDisplayNameAsync(cancellationToken)) ?? string.Empty;
        DateOnly validUntil = OperationsTimeZones.Today(quote.ValidUntilUtc, settings?.TimeZone);
        OperationsQuoteDocument document = new(quote, company, customer.FullName, customer.Code, customer.Email.Value,
            quote.OriginWarehouseId is { } from && warehouses.TryGetValue(from, out OperationsWarehouse? origin) ? origin.Name : null,
            quote.DestinationWarehouseId is { } to && warehouses.TryGetValue(to, out OperationsWarehouse? destination) ? destination.Name : null,
            settings?.WeightUnit ?? OperationsWeightUnit.Kilogram, settings?.DimensionUnit ?? OperationsDimensionUnit.Centimetre,
            OperationsTimeZones.Today(quote.CreatedUtc, settings?.TimeZone), validUntil);
        CultureInfo before = CultureInfo.CurrentUICulture;

        try
        {
            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo(customer.PreferredLanguage.Value);
            return (_renderer.RenderQuote(document), customer, company, validUntil);
        }
        finally
        {
            CultureInfo.CurrentUICulture = before;
        }
    }
}
