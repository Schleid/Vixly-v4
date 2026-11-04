using Microsoft.AspNetCore.DataProtection.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Vixly.Operations.Application.Ports;
using Vixly.Operations.Domain.Access;
using Vixly.Operations.Domain.Audit;
using Vixly.Operations.Domain.Branding;
using Vixly.Operations.Domain.Common;
using Vixly.Operations.Domain.Identity;
using Vixly.Operations.Domain.Jobs;
using Vixly.Operations.Domain.Locations;
using Vixly.Operations.Domain.Messaging;
using Vixly.Operations.Domain.Platform;
using Vixly.Operations.Infrastructure.Persistence.Conventions;

namespace Vixly.Operations.Infrastructure.Persistence;

// =========================================================
// FILE: OperationsDbContext.cs
//
// PURPOSE:
// The Operations database: schema ops (D-1), filtered by tenant (Stage 3, Slice 0, D-F1).
//
// RESPONSIBILITIES:
// - Expose the tables
// - Filter every tenant query by the scope tenant
// - Refuse saving a tenant row that belongs to another tenant
//
// IMPORTANT NOTES:
// - Without an entered tenant or system scope, a tenant query throws instead of returning every tenant
// - The Data Protection key ring lives here too (S-5)
// - Performance plan P-3: a few lookups (the staff member, rights, warehouses, settings, brand) are kept for the rest of this context
//   only, per tenant or system scope, and forgotten at every save or cleared tracker
// =========================================================
public sealed class OperationsDbContext : DbContext, IDataProtectionKeyContext
{
    private readonly IOperationsTenantScope _scope;

    private readonly Action<ModelBuilder>? _testModel;

    // P-3: the lookups this context already made, by scope and key (a context is one request or one job item).
    private readonly Dictionary<string, object?> _remembered = new(StringComparer.Ordinal);

    public OperationsDbContext(DbContextOptions<OperationsDbContext> options, IOperationsTenantScope scope)
        : base(options)
    {
        _scope = scope ?? throw new ArgumentNullException(nameof(scope));
    }

    // Tests only: adds a tenant entity so isolation is proven before a real one exists (Slice 0 D-F1).
    internal OperationsDbContext(DbContextOptions<OperationsDbContext> options, IOperationsTenantScope scope, Action<ModelBuilder> testModel)
        : this(options, scope)
    {
        _testModel = testModel ?? throw new ArgumentNullException(nameof(testModel));
    }

    internal bool HasTestModel => _testModel is not null;

    public DbSet<OperationsOutboxMessage> OutboxMessages => Set<OperationsOutboxMessage>();

    public DbSet<OperationsJobRun> JobRuns => Set<OperationsJobRun>();

    public DbSet<OperationsPlatformFeedPosition> PlatformFeedPositions => Set<OperationsPlatformFeedPosition>();

    public DbSet<OperationsUser> Users => Set<OperationsUser>();

    public DbSet<OperationsUserSession> UserSessions => Set<OperationsUserSession>();

    public DbSet<OperationsDeviceBlock> DeviceBlocks => Set<OperationsDeviceBlock>();

    public DbSet<OperationsStaffBadge> StaffBadges => Set<OperationsStaffBadge>();

    public DbSet<Vixly.Operations.Domain.Receiving.OperationsShelfCount> ShelfCounts => Set<Vixly.Operations.Domain.Receiving.OperationsShelfCount>();

    public DbSet<OperationsStaffPin> StaffPins => Set<OperationsStaffPin>();

    public DbSet<OperationsDevicePerson> DevicePeople => Set<OperationsDevicePerson>();

    public DbSet<OperationsSignInChallenge> SignInChallenges => Set<OperationsSignInChallenge>();

    public DbSet<OperationsAccountToken> AccountTokens => Set<OperationsAccountToken>();

    public DbSet<OperationsAuditEntry> AuditEntries => Set<OperationsAuditEntry>();

    public DbSet<OperationsTenantBrand> TenantBrands => Set<OperationsTenantBrand>();

    public DbSet<OperationsSiteImage> SiteImages => Set<OperationsSiteImage>();

    public DbSet<OperationsBranch> Branches => Set<OperationsBranch>();

    public DbSet<OperationsWarehouse> Warehouses => Set<OperationsWarehouse>();

    public DbSet<OperationsCountryContact> CountryContacts => Set<OperationsCountryContact>();

    public DbSet<Vixly.Operations.Domain.Branding.OperationsSiteTestimonial> SiteTestimonials => Set<Vixly.Operations.Domain.Branding.OperationsSiteTestimonial>();

    public DbSet<Vixly.Operations.Domain.Items.OperationsParcelItem> ParcelItems => Set<Vixly.Operations.Domain.Items.OperationsParcelItem>();

    public DbSet<Vixly.Operations.Domain.Items.OperationsItemCatalogueEntry> ItemCatalogue => Set<Vixly.Operations.Domain.Items.OperationsItemCatalogueEntry>();

    public DbSet<OperationsRole> Roles => Set<OperationsRole>();

    public DbSet<OperationsRoleGrant> RoleGrants => Set<OperationsRoleGrant>();

    public DbSet<Vixly.Operations.Domain.Customers.OperationsCustomer> Customers => Set<Vixly.Operations.Domain.Customers.OperationsCustomer>();

    public DbSet<Vixly.Operations.Domain.Customers.OperationsCustomerSession> CustomerSessions => Set<Vixly.Operations.Domain.Customers.OperationsCustomerSession>();

    public DbSet<Vixly.Operations.Domain.Customers.OperationsCustomerToken> CustomerTokens => Set<Vixly.Operations.Domain.Customers.OperationsCustomerToken>();

    public DbSet<Vixly.Operations.Domain.Tracking.OperationsTrackingEvent> TrackingEvents => Set<Vixly.Operations.Domain.Tracking.OperationsTrackingEvent>();

    public DbSet<Vixly.Operations.Domain.Tracking.OperationsParcel> Parcels => Set<Vixly.Operations.Domain.Tracking.OperationsParcel>();

    public DbSet<Vixly.Operations.Domain.Receiving.OperationsPreAlert> PreAlerts => Set<Vixly.Operations.Domain.Receiving.OperationsPreAlert>();

    public DbSet<Vixly.Operations.Domain.Shipments.OperationsShipment> Shipments => Set<Vixly.Operations.Domain.Shipments.OperationsShipment>();

    public DbSet<Vixly.Operations.Domain.Receiving.OperationsWarehouseReceipt> WarehouseReceipts => Set<Vixly.Operations.Domain.Receiving.OperationsWarehouseReceipt>();

    public DbSet<Vixly.Operations.Domain.Receiving.OperationsParcelPhoto> ParcelPhotos => Set<Vixly.Operations.Domain.Receiving.OperationsParcelPhoto>();

    public DbSet<Vixly.Operations.Domain.Receiving.OperationsStorageLocation> StorageLocations => Set<Vixly.Operations.Domain.Receiving.OperationsStorageLocation>();

    public DbSet<Vixly.Operations.Domain.Receiving.OperationsInboundDelivery> InboundDeliveries => Set<Vixly.Operations.Domain.Receiving.OperationsInboundDelivery>();

    public DbSet<Vixly.Operations.Domain.Printing.OperationsLabelPrinter> LabelPrinters => Set<Vixly.Operations.Domain.Printing.OperationsLabelPrinter>();

    public DbSet<Vixly.Operations.Domain.Printing.OperationsPrintAgent> PrintAgents => Set<Vixly.Operations.Domain.Printing.OperationsPrintAgent>();

    public DbSet<Vixly.Operations.Domain.Printing.OperationsPrintJob> PrintJobs => Set<Vixly.Operations.Domain.Printing.OperationsPrintJob>();

    public DbSet<Vixly.Operations.Domain.Printing.OperationsPrintPairing> PrintPairings => Set<Vixly.Operations.Domain.Printing.OperationsPrintPairing>();

    public DbSet<Vixly.Operations.Domain.Settings.OperationsNumberSequence> NumberSequences => Set<Vixly.Operations.Domain.Settings.OperationsNumberSequence>();

    public DbSet<Vixly.Operations.Domain.Settings.OperationsShippingServiceSetup> ShippingServiceSetups => Set<Vixly.Operations.Domain.Settings.OperationsShippingServiceSetup>();

    public DbSet<Vixly.Operations.Domain.Settings.OperationsTenantSettings> TenantSettings => Set<Vixly.Operations.Domain.Settings.OperationsTenantSettings>();

    public DbSet<Vixly.Operations.Domain.Inspection.OperationsRestrictedItem> RestrictedItems => Set<Vixly.Operations.Domain.Inspection.OperationsRestrictedItem>();

    public DbSet<Vixly.Operations.Domain.Inspection.OperationsInspectionRule> InspectionRules => Set<Vixly.Operations.Domain.Inspection.OperationsInspectionRule>();

    public DbSet<Vixly.Operations.Domain.Inspection.OperationsInspection> Inspections => Set<Vixly.Operations.Domain.Inspection.OperationsInspection>();

    public DbSet<Vixly.Operations.Domain.Holds.OperationsHoldAnswer> HoldAnswers => Set<Vixly.Operations.Domain.Holds.OperationsHoldAnswer>();

    public DbSet<Vixly.Operations.Domain.Charges.OperationsRateCard> RateCards => Set<Vixly.Operations.Domain.Charges.OperationsRateCard>();

    public DbSet<Vixly.Operations.Domain.Charges.OperationsChargeExtra> ChargeExtras => Set<Vixly.Operations.Domain.Charges.OperationsChargeExtra>();

    public DbSet<Vixly.Operations.Domain.Charges.OperationsInsurancePlan> InsurancePlans => Set<Vixly.Operations.Domain.Charges.OperationsInsurancePlan>();

    public DbSet<Vixly.Operations.Domain.Charges.OperationsQuote> Quotes => Set<Vixly.Operations.Domain.Charges.OperationsQuote>();

    public DbSet<Vixly.Operations.Domain.Charges.OperationsItemType> ItemTypes => Set<Vixly.Operations.Domain.Charges.OperationsItemType>();

    // Slice 5E: the shipping supplies the company sells, and their barcodes.
    public DbSet<Vixly.Operations.Domain.Supplies.OperationsSupply> Supplies => Set<Vixly.Operations.Domain.Supplies.OperationsSupply>();

    public DbSet<Vixly.Operations.Domain.Supplies.OperationsSupplyBarcode> SupplyBarcodes => Set<Vixly.Operations.Domain.Supplies.OperationsSupplyBarcode>();

    // Slice 5E step 2: the supplies stock: movements, the balance per warehouse, transfers and counts.
    public DbSet<Vixly.Operations.Domain.Supplies.OperationsSupplyMovement> SupplyMovements => Set<Vixly.Operations.Domain.Supplies.OperationsSupplyMovement>();

    public DbSet<Vixly.Operations.Domain.Supplies.OperationsSupplyStock> SupplyStocks => Set<Vixly.Operations.Domain.Supplies.OperationsSupplyStock>();

    public DbSet<Vixly.Operations.Domain.Supplies.OperationsSupplyTransfer> SupplyTransfers => Set<Vixly.Operations.Domain.Supplies.OperationsSupplyTransfer>();

    public DbSet<Vixly.Operations.Domain.Supplies.OperationsSupplyCount> SupplyCounts => Set<Vixly.Operations.Domain.Supplies.OperationsSupplyCount>();

    // Slice 5E step 3: supplies sold at the counter.
    public DbSet<Vixly.Operations.Domain.Supplies.OperationsSupplySale> SupplySales => Set<Vixly.Operations.Domain.Supplies.OperationsSupplySale>();

    // Slice 5F step 1: invoices and their credit notes.
    public DbSet<Vixly.Operations.Domain.Billing.OperationsInvoice> Invoices => Set<Vixly.Operations.Domain.Billing.OperationsInvoice>();

    public DbSet<Vixly.Operations.Domain.Billing.OperationsCreditNote> CreditNotes => Set<Vixly.Operations.Domain.Billing.OperationsCreditNote>();

    // Slice 5F step 2: payments, cash drawers and refunds.
    public DbSet<Vixly.Operations.Domain.Billing.OperationsPayment> Payments => Set<Vixly.Operations.Domain.Billing.OperationsPayment>();

    public DbSet<Vixly.Operations.Domain.Billing.OperationsCashDrawer> CashDrawers => Set<Vixly.Operations.Domain.Billing.OperationsCashDrawer>();

    public DbSet<Vixly.Operations.Domain.Billing.OperationsRefund> Refunds => Set<Vixly.Operations.Domain.Billing.OperationsRefund>();

    // Slice 5F step 4: the company's Stripe account, its card readers, and card charges.
    public DbSet<Vixly.Operations.Domain.Billing.OperationsCardProvider> CardProviders => Set<Vixly.Operations.Domain.Billing.OperationsCardProvider>();

    public DbSet<Vixly.Operations.Domain.Billing.OperationsCardReader> CardReaders => Set<Vixly.Operations.Domain.Billing.OperationsCardReader>();

    public DbSet<Vixly.Operations.Domain.Billing.OperationsCardCharge> CardCharges => Set<Vixly.Operations.Domain.Billing.OperationsCardCharge>();

    public DbSet<Vixly.Operations.Domain.Screens.OperationsCustomerScreen> CustomerScreens => Set<Vixly.Operations.Domain.Screens.OperationsCustomerScreen>();

    public DbSet<Vixly.Operations.Domain.Screens.OperationsScreenRecord> ScreenRecords => Set<Vixly.Operations.Domain.Screens.OperationsScreenRecord>();

    public DbSet<Vixly.Operations.Domain.Payables.OperationsVendor> Vendors => Set<Vixly.Operations.Domain.Payables.OperationsVendor>();

    public DbSet<Vixly.Operations.Domain.Payables.OperationsBill> Bills => Set<Vixly.Operations.Domain.Payables.OperationsBill>();

    public DbSet<Vixly.Operations.Domain.Texts.OperationsTextProvider> TextProviders => Set<Vixly.Operations.Domain.Texts.OperationsTextProvider>();

    public DbSet<Vixly.Operations.Domain.Texts.OperationsTextMessage> TextMessages => Set<Vixly.Operations.Domain.Texts.OperationsTextMessage>();

    public DbSet<Vixly.Operations.Domain.Reports.OperationsReportSchedule> ReportSchedules => Set<Vixly.Operations.Domain.Reports.OperationsReportSchedule>();

    public DbSet<Vixly.Operations.Domain.Api.OperationsApiEvent> ApiEvents => Set<Vixly.Operations.Domain.Api.OperationsApiEvent>();

    public DbSet<Vixly.Operations.Domain.Api.OperationsApiKey> ApiKeys => Set<Vixly.Operations.Domain.Api.OperationsApiKey>();

    public DbSet<Vixly.Operations.Domain.Api.OperationsWebhookEndpoint> WebhookEndpoints => Set<Vixly.Operations.Domain.Api.OperationsWebhookEndpoint>();

    public DbSet<Vixly.Operations.Domain.Api.OperationsWebhookDelivery> WebhookDeliveries => Set<Vixly.Operations.Domain.Api.OperationsWebhookDelivery>();

    // Slice 5F step 2b: customers' credit accounts.
    public DbSet<Vixly.Operations.Domain.Billing.OperationsCreditAccount> CreditAccounts => Set<Vixly.Operations.Domain.Billing.OperationsCreditAccount>();

    // Slice 6A: bundles and repack boxes.
    public DbSet<Vixly.Operations.Domain.Bundles.OperationsBundle> Bundles => Set<Vixly.Operations.Domain.Bundles.OperationsBundle>();

    public DbSet<Vixly.Operations.Domain.Bundles.OperationsRepackBox> RepackBoxes => Set<Vixly.Operations.Domain.Bundles.OperationsRepackBox>();

    // A bundle's list, confirmed by scans (owner, 2026-10-21).
    public DbSet<Vixly.Operations.Domain.Bundles.OperationsBundleItem> BundleItems => Set<Vixly.Operations.Domain.Bundles.OperationsBundleItem>();

    // Slice 6B: carriers, their rates and dispatches.
    public DbSet<Vixly.Operations.Domain.Dispatch.OperationsCarrier> Carriers => Set<Vixly.Operations.Domain.Dispatch.OperationsCarrier>();

    public DbSet<Vixly.Operations.Domain.Dispatch.OperationsCarrierRate> CarrierRates => Set<Vixly.Operations.Domain.Dispatch.OperationsCarrierRate>();

    public DbSet<Vixly.Operations.Domain.Dispatch.OperationsDispatch> Dispatches => Set<Vixly.Operations.Domain.Dispatch.OperationsDispatch>();

    public DbSet<Vixly.Operations.Domain.Dispatch.OperationsDispatchBundle> DispatchBundles => Set<Vixly.Operations.Domain.Dispatch.OperationsDispatchBundle>();

    // Slice 7 step 2: releases at the pickup counter.
    public DbSet<Vixly.Operations.Domain.Pickup.OperationsPickupRelease> PickupReleases => Set<Vixly.Operations.Domain.Pickup.OperationsPickupRelease>();

    // Slice 7 step 3: home delivery.
    public DbSet<Vixly.Operations.Domain.Delivery.OperationsDeliveryRoute> DeliveryRoutes => Set<Vixly.Operations.Domain.Delivery.OperationsDeliveryRoute>();

    public DbSet<Vixly.Operations.Domain.Delivery.OperationsDeliveryStop> DeliveryStops => Set<Vixly.Operations.Domain.Delivery.OperationsDeliveryStop>();

    public DbSet<Vixly.Operations.Domain.Delivery.OperationsDriverPosition> DriverPositions => Set<Vixly.Operations.Domain.Delivery.OperationsDriverPosition>();

    // Slice 7: what did not match at a destination.
    public DbSet<Vixly.Operations.Domain.Arrivals.OperationsArrivalDiscrepancy> ArrivalDiscrepancies => Set<Vixly.Operations.Domain.Arrivals.OperationsArrivalDiscrepancy>();

    public DbSet<Vixly.Operations.Domain.Dispatch.OperationsDispatchShipment> DispatchShipments => Set<Vixly.Operations.Domain.Dispatch.OperationsDispatchShipment>();

    // Slice 6C: the company's own causes of delays and exceptions.
    public DbSet<Vixly.Operations.Domain.Events.OperationsEventReason> EventReasons => Set<Vixly.Operations.Domain.Events.OperationsEventReason>();

    // Slice 4D step 3: the counts of the undeclared parcels area.
    public DbSet<Vixly.Operations.Domain.Holds.OperationsStockCount> StockCounts => Set<Vixly.Operations.Domain.Holds.OperationsStockCount>();

    // Slice 4D step 4: the customers' tickets about missing packages.
    public DbSet<Vixly.Operations.Domain.Tickets.OperationsTicket> Tickets => Set<Vixly.Operations.Domain.Tickets.OperationsTicket>();

    // Slice 5B: the company's insurance policies, its liability terms, the certificates of insured shipments.
    public DbSet<Vixly.Operations.Domain.Insurance.OperationsInsurancePolicy> InsurancePolicies => Set<Vixly.Operations.Domain.Insurance.OperationsInsurancePolicy>();

    public DbSet<Vixly.Operations.Domain.Insurance.OperationsLiabilityTerms> LiabilityTerms => Set<Vixly.Operations.Domain.Insurance.OperationsLiabilityTerms>();

    public DbSet<Vixly.Operations.Domain.Insurance.OperationsInsuranceCertificate> InsuranceCertificates => Set<Vixly.Operations.Domain.Insurance.OperationsInsuranceCertificate>();

    public DbSet<Microsoft.AspNetCore.DataProtection.EntityFrameworkCore.DataProtectionKey> DataProtectionKeys => Set<Microsoft.AspNetCore.DataProtection.EntityFrameworkCore.DataProtectionKey>();

    // Read by the query filter on every query (OperationsModelConventions).
    internal bool IgnoreTenantFilter => _scope.IsSystem;

    internal Guid TenantFilterId => _scope.TenantId
        ?? (_scope.IsSystem ? Guid.Empty : throw new InvalidOperationException("Tenant data was queried outside a tenant scope (D-F1)."));

    // =====================================================
    // METHOD: REMEMBER
    // Performance plan P-3 (owner, 2026-11-04): a lookup repeated in the same request is answered from the first one.
    // The key carries the scope, so a company's answer is never reused in another company or in system work;
    // every save and every cleared tracker forgets everything, so a change is always read again.
    // Only for tracked lookups whose answer the same context would give again (EF returns the tracked rows).
    // =====================================================
    internal async Task<T> RememberAsync<T>(string key, Func<Task<T>> load)
    {
        ArgumentNullException.ThrowIfNull(load);

        string scoped = (_scope.IsSystem ? "system|" : "tenant|") + (_scope.TenantId?.ToString("N") ?? "-") + "|" + key;

        if (_remembered.TryGetValue(scoped, out object? kept))
        {
            return (T)kept!;
        }

        T loaded = await load();
        _remembered[scoped] = loaded;
        return loaded;
    }

    // What this context already remembered under the key, if anything (harmonization H-5: a row by id found in a list already read).
    internal bool TryRemembered<T>(string key, out T value)
    {
        string scoped = (_scope.IsSystem ? "system|" : "tenant|") + (_scope.TenantId?.ToString("N") ?? "-") + "|" + key;

        if (_remembered.TryGetValue(scoped, out object? kept) && kept is T found)
        {
            value = found;
            return true;
        }

        value = default!;
        return false;
    }

    // The tracker was cleared (a clash or discarded changes): what was remembered is no longer tracked.
    internal void Forget() => _remembered.Clear();

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        try
        {
            GiveTrackingNumbersAsync(false, CancellationToken.None).GetAwaiter().GetResult();
            RecordApiEvents();
            CheckTenantOfChanges();
            return base.SaveChanges(acceptAllChangesOnSuccess);
        }
        finally
        {
            Forget();
        }
    }

    public override async Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        try
        {
            await GiveTrackingNumbersAsync(true, cancellationToken);
            RecordApiEvents();
            CheckTenantOfChanges();
            return await base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
        }
        finally
        {
            Forget();
        }
    }

    // =====================================================
    // METHOD: GIVE TRACKING NUMBERS
    // Owner, 2026-10-28: a shipment gets its tracking number in the same save that makes it paid, charged to an account
    // or collect, whichever screen or webhook did it. The housekeeping round gives any that were missed.
    // =====================================================
    private async Task GiveTrackingNumbersAsync(bool isAsync, CancellationToken cancellationToken)
    {
        Dictionary<Guid, DateTimeOffset> due = new();

        foreach (Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry<Vixly.Operations.Domain.Billing.OperationsInvoice> entry in ChangeTracker.Entries<Vixly.Operations.Domain.Billing.OperationsInvoice>())
        {
            if (entry.State is EntityState.Added or EntityState.Modified && entry.Entity.ShipmentId is { } shipmentId && entry.Entity.Released)
            {
                due[shipmentId] = entry.Entity.LastModifiedUtc ?? entry.Entity.CreatedUtc;
            }
        }

        foreach (Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry<Vixly.Operations.Domain.Shipments.OperationsShipment> entry in ChangeTracker.Entries<Vixly.Operations.Domain.Shipments.OperationsShipment>())
        {
            if (entry.State is EntityState.Added or EntityState.Modified && entry.Entity.CollectReadyForTracking)
            {
                due[entry.Entity.Id] = entry.Entity.LastModifiedUtc ?? entry.Entity.CreatedUtc;
            }
        }

        foreach ((Guid shipmentId, DateTimeOffset utcNow) in due)
        {
            Vixly.Operations.Domain.Shipments.OperationsShipment? shipment = Shipments.Local.FirstOrDefault(item => item.Id == shipmentId)
                ?? (isAsync
                    ? await Shipments.FirstOrDefaultAsync(item => item.Id == shipmentId, cancellationToken)
                    : Shipments.FirstOrDefault(item => item.Id == shipmentId));

            if (shipment is not null)
            {
                await GiveTrackingNumberAsync(shipment, utcNow, isAsync, cancellationToken);
            }
        }
    }

    // One shipment, booked and still without a number; also used by the housekeeping round (OperationsTrackingNumberBackfill).
    internal async Task GiveTrackingNumberAsync(Vixly.Operations.Domain.Shipments.OperationsShipment shipment, DateTimeOffset utcNow, bool isAsync, CancellationToken cancellationToken)
    {
        if (shipment.TrackingNumber is not null || !shipment.IsBooked)
        {
            return;
        }

        Vixly.Operations.Domain.Settings.OperationsTenantSettings? settings = TenantSettings.Local.FirstOrDefault(item => item.TenantId == shipment.TenantId)
            ?? (isAsync
                ? await TenantSettings.FirstOrDefaultAsync(item => item.TenantId == shipment.TenantId, cancellationToken)
                : TenantSettings.FirstOrDefault(item => item.TenantId == shipment.TenantId));
        string prefix = settings?.TrackingPrefix ?? Vixly.Operations.Domain.Shipments.OperationsTrackingNumbers.DefaultPrefix;

        for (int attempt = 0; ; attempt++)
        {
            string number = OperationsTrackingNumberSource.Next(prefix);
            // Within the company; the unique index guards across companies (ten random digits make a clash practically impossible).
            bool taken = Shipments.Local.Any(item => item.TrackingNumber == number)
                || (isAsync
                    ? await Shipments.AnyAsync(item => item.TrackingNumber == number, cancellationToken)
                    : Shipments.Any(item => item.TrackingNumber == number));

            if (!taken)
            {
                shipment.GiveTrackingNumber(number, utcNow);
                return;
            }

            if (attempt >= 5)
            {
                throw new InvalidOperationException("No free tracking number after several tries.");
            }
        }
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);

        modelBuilder.HasDefaultSchema(OperationsDatabase.Schema);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(OperationsDbContext).Assembly);
        _testModel?.Invoke(modelBuilder);
        OperationsModelConventions.Apply(modelBuilder, this);
    }

    // =====================================================
    // METHOD: RECORD API EVENTS
    // Slice 10 (review): what the company's webhooks send is written in the same save as the change itself.
    // =====================================================
    private void RecordApiEvents()
    {
        List<Vixly.Operations.Domain.Api.OperationsApiEvent> events = new();

        foreach (Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry<OperationsEntity> entry in ChangeTracker.Entries<OperationsEntity>())
        {
            if (entry.State != EntityState.Added)
            {
                continue;
            }

            Vixly.Operations.Domain.Api.OperationsWebhookEvents kind = entry.Entity switch
            {
                Vixly.Operations.Domain.Tracking.OperationsTrackingEvent tracking
                    when tracking.SubjectKind is Vixly.Operations.Domain.Tracking.OperationsTrackingSubject.Parcel or Vixly.Operations.Domain.Tracking.OperationsTrackingSubject.Shipment
                    => Vixly.Operations.Domain.Api.OperationsWebhookEvents.Tracking,
                Vixly.Operations.Domain.Customers.OperationsCustomer => Vixly.Operations.Domain.Api.OperationsWebhookEvents.CustomerCreated,
                Vixly.Operations.Domain.Billing.OperationsInvoice => Vixly.Operations.Domain.Api.OperationsWebhookEvents.InvoiceIssued,
                Vixly.Operations.Domain.Billing.OperationsPayment => Vixly.Operations.Domain.Api.OperationsWebhookEvents.PaymentRecorded,
                _ => Vixly.Operations.Domain.Api.OperationsWebhookEvents.None
            };

            if (kind != Vixly.Operations.Domain.Api.OperationsWebhookEvents.None)
            {
                events.Add(Vixly.Operations.Domain.Api.OperationsApiEvent.Record(entry.Entity.TenantId, kind, entry.Entity.Id, entry.Entity.CreatedUtc));
            }
        }

        if (events.Count > 0)
        {
            ApiEvents.AddRange(events);
        }
    }

    // =====================================================
    // METHOD: CHECK TENANT OF CHANGES
    // =====================================================
    private void CheckTenantOfChanges()
    {
        if (_scope.IsSystem)
        {
            return;
        }

        foreach (Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry<OperationsEntity> entry in ChangeTracker.Entries<OperationsEntity>())
        {
            if (entry.State is EntityState.Added or EntityState.Modified or EntityState.Deleted
                && entry.Entity.TenantId != (_scope.TenantId ?? Guid.Empty))
            {
                throw new InvalidOperationException("A change to another tenant's data was refused (D-F1).");
            }
        }
    }
}
