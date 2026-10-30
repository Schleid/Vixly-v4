using OperationsInvoice = Vixly.Operations.Domain.Billing.OperationsInvoice;
using Vixly.Operations.Application.Bundles;
using Vixly.Operations.Application.Ports;
using Vixly.Operations.Application.Security;
using Vixly.Operations.Domain.Access;
using Vixly.Operations.Domain.Billing;
using Vixly.Operations.Domain.Locations;
using Vixly.Operations.Domain.Screens;
using Vixly.Operations.Domain.Settings;
using Vixly.Operations.Domain.Shipments;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.Screens;

// =========================================================
// FILE: OperationsScreenDesk.cs
//
// PURPOSE:
// What every customer-screen use case shares (Slice 5F step 3).
//
// RESPONSIBILITIES:
// - Open the screens of the warehouses where the person serves at the counter or takes payments
// - Find a screen from its own secret (its cookie), and note that it was seen
//
// IMPORTANT NOTES:
// - A screen is found only by the hash of its secret; the secret itself is never stored
// =========================================================
public sealed class OperationsScreenDesk
{
    private readonly OperationsBundleDesk _bundles;

    private readonly IOperationsScreenRepository _screens;

    private readonly IOperationsSecretGenerator _secrets;

    private readonly IOperationsLocationRepository _locations;

    private readonly IOperationsBillingRepository _billing;

    private readonly IOperationsCardRepository _cards;

    private readonly IOperationsReceivingRepository _receiving;

    public OperationsScreenDesk(OperationsBundleDesk bundles, IOperationsScreenRepository screens, IOperationsSecretGenerator secrets, IOperationsLocationRepository locations,
        IOperationsBillingRepository billing, IOperationsCardRepository cards, IOperationsReceivingRepository receiving)
    {
        _receiving = receiving ?? throw new ArgumentNullException(nameof(receiving));
        _billing = billing ?? throw new ArgumentNullException(nameof(billing));
        _cards = cards ?? throw new ArgumentNullException(nameof(cards));
        _bundles = bundles ?? throw new ArgumentNullException(nameof(bundles));
        _screens = screens ?? throw new ArgumentNullException(nameof(screens));
        _secrets = secrets ?? throw new ArgumentNullException(nameof(secrets));
        _locations = locations ?? throw new ArgumentNullException(nameof(locations));
    }

    public IOperationsScreenRepository Screens => _screens;

    public Task<(OperationsAccessView? Access, IReadOnlyList<OperationsWarehouse> Warehouses, Error? Refused)> OpenAsync(OperationsAuthorizationContext context,
        CancellationToken cancellationToken) =>
        _bundles.OpenAsync(context, new[] { OperationsPermissions.CounterServe, OperationsPermissions.PaymentsTake }, cancellationToken);

    public async Task<OperationsCustomerScreen?> FromSecretAsync(string? secret, CancellationToken cancellationToken) =>
        string.IsNullOrWhiteSpace(secret) || secret.Length > 200 ? null : await _screens.FindBySecretAsync(_secrets.HashToken(secret), cancellationToken);

    public async Task<IReadOnlyList<OperationsScreenRow>> RowsAsync(IReadOnlyList<OperationsWarehouse> warehouses, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(warehouses);
        Dictionary<Guid, string> names = warehouses.ToDictionary(warehouse => warehouse.Id, warehouse => warehouse.Name);
        return (await _screens.ListAsync(cancellationToken)).Where(screen => names.ContainsKey(screen.WarehouseId))
            .Select(screen => new OperationsScreenRow(screen.Id, screen.Name, screen.WarehouseId, names[screen.WarehouseId], screen.Paired, screen.LastSeenUtc, screen.Step,
                screen.InvoiceId, screen.ShipmentId, screen.ZelleAcceptedUtc))
            .ToList();
    }

    public async Task<OperationsScreenState> StateAsync(OperationsCustomerScreen screen, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(screen);
        OperationsInvoice? invoice = screen.InvoiceId is { } invoiceId ? await _billing.GetInvoiceAsync(invoiceId, cancellationToken) : null;
        OperationsCardCharge? charge = screen.CardChargeId is { } chargeId ? await _cards.GetChargeAsync(chargeId, cancellationToken) : null;
        OperationsShipment? shipment = screen.ShipmentId is { } shipmentId ? await _receiving.GetShipmentAsync(shipmentId, cancellationToken) : null;
        return new OperationsScreenState(invoice, charge, shipment, Version(screen, invoice, charge, shipment));
    }

    public static string Version(OperationsCustomerScreen screen, OperationsInvoice? invoice, OperationsCardCharge? charge, OperationsShipment? shipment)
    {
        ArgumentNullException.ThrowIfNull(screen);
        return string.Join(':', (int)screen.Step, screen.LastModifiedUtc?.UtcTicks ?? 0, invoice?.LastModifiedUtc?.UtcTicks ?? 0, (int?)charge?.Status ?? 0,
            shipment?.LastModifiedUtc?.UtcTicks ?? 0);
    }

    // The Zelle terms as the customer reads them, as a hash: an acceptance counts only for the terms read.
    public string ZelleTermsHash(OperationsTenantSettings settings, string? language)
    {
        ArgumentNullException.ThrowIfNull(settings);
        return _secrets.HashToken(settings.ZelleTerms(language) ?? string.Empty);
    }

    public Task<OperationsWarehouse?> WarehouseAsync(Guid warehouseId, CancellationToken cancellationToken) => _locations.GetWarehouseAsync(warehouseId, cancellationToken);
}
