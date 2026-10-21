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
// =========================================================
public sealed class OperationsDbContext : DbContext, IDataProtectionKeyContext
{
    private readonly IOperationsTenantScope _scope;

    private readonly Action<ModelBuilder>? _testModel;

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

    public DbSet<OperationsSignInChallenge> SignInChallenges => Set<OperationsSignInChallenge>();

    public DbSet<OperationsAccountToken> AccountTokens => Set<OperationsAccountToken>();

    public DbSet<OperationsAuditEntry> AuditEntries => Set<OperationsAuditEntry>();

    public DbSet<OperationsTenantBrand> TenantBrands => Set<OperationsTenantBrand>();

    public DbSet<OperationsSiteImage> SiteImages => Set<OperationsSiteImage>();

    public DbSet<OperationsBranch> Branches => Set<OperationsBranch>();

    public DbSet<OperationsWarehouse> Warehouses => Set<OperationsWarehouse>();

    public DbSet<OperationsCountryContact> CountryContacts => Set<OperationsCountryContact>();

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

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        CheckTenantOfChanges();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        CheckTenantOfChanges();
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
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
