using Microsoft.AspNetCore.DataProtection.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Vixly.Platform.Domain.Audit;
using Vixly.Platform.Domain.Identity;
using Vixly.Platform.Domain.Messaging;
using Vixly.Platform.Domain.Sales;
using Vixly.Platform.Domain.Subscriptions;
using Vixly.Platform.Domain.Tenants;
using Vixly.Platform.Infrastructure.Persistence.Conventions;

namespace Vixly.Platform.Infrastructure.Persistence;

// =========================================================
// FILE: PlatformDbContext.cs
//
// PURPOSE:
// The Platform plane's unit of work over the platform schema.
//
// RESPONSIBILITIES:
// - Place every Platform table in the platform schema
// - Expose the aggregate roots as DbSets (named without the
//   plane prefix, Global Standard 4.1)
// - Store the Data Protection key ring (S-5)
// - Apply the entity configurations in this assembly
// - Apply the application-assigned key convention (D-5)
//
// IMPORTANT NOTES:
// - No foreign key, view or query may reach another schema
//   (D-1)
// - Never migrated at startup; migrations ship as a bundle
//   applied by the pipeline
// =========================================================
public sealed class PlatformDbContext : DbContext, IDataProtectionKeyContext
{
    public PlatformDbContext(DbContextOptions<PlatformDbContext> options)
        : base(options)
    {
    }

    public DbSet<PlatformUser> Users => Set<PlatformUser>();

    public DbSet<PlatformRole> Roles => Set<PlatformRole>();

    public DbSet<PlatformDepartment> Departments => Set<PlatformDepartment>();

    public DbSet<PlatformUserSession> UserSessions => Set<PlatformUserSession>();

    public DbSet<PlatformAccountToken> AccountTokens => Set<PlatformAccountToken>();

    public DbSet<PlatformSignInChallenge> SignInChallenges => Set<PlatformSignInChallenge>();

    public DbSet<PlatformPasskeyCeremony> PasskeyCeremonies => Set<PlatformPasskeyCeremony>();

    public DbSet<PlatformPasswordResetRequest> PasswordResetRequests => Set<PlatformPasswordResetRequest>();

    public DbSet<PlatformAuditEntry> AuditEntries => Set<PlatformAuditEntry>();

    public DbSet<PlatformTenant> Tenants => Set<PlatformTenant>();

    public DbSet<PlatformTenantLifecycleEvent> TenantLifecycleEvents => Set<PlatformTenantLifecycleEvent>();

    public DbSet<PlatformTenantNote> TenantNotes => Set<PlatformTenantNote>();

    public DbSet<PlatformPortalSlug> PortalSlugs => Set<PlatformPortalSlug>();

    public DbSet<PlatformTenantRegistration> TenantRegistrations => Set<PlatformTenantRegistration>();

    public DbSet<PlatformRegistrationToken> RegistrationTokens => Set<PlatformRegistrationToken>();

    public DbSet<PlatformOutboxMessage> OutboxMessages => Set<PlatformOutboxMessage>();

    public DbSet<PlatformPublishedEvent> PublishedEvents => Set<PlatformPublishedEvent>();

    public DbSet<Vixly.Platform.Domain.Sales.PlatformSalesLead> SalesLeads => Set<Vixly.Platform.Domain.Sales.PlatformSalesLead>();

    public DbSet<Vixly.Platform.Domain.Tenants.PlatformTenantChangeRequest> TenantChangeRequests => Set<Vixly.Platform.Domain.Tenants.PlatformTenantChangeRequest>();

    public DbSet<Vixly.Platform.Domain.Tenants.PlatformTenantChangeProof> TenantChangeProofs => Set<Vixly.Platform.Domain.Tenants.PlatformTenantChangeProof>();

    public DbSet<PlatformPlan> Plans => Set<PlatformPlan>();

    public DbSet<PlatformSubscription> Subscriptions => Set<PlatformSubscription>();

    public DbSet<PlatformSubscriptionEvent> SubscriptionEvents => Set<PlatformSubscriptionEvent>();

    public DbSet<PlatformTenantLimitOverride> TenantLimitOverrides => Set<PlatformTenantLimitOverride>();

    public DbSet<PlatformSalesSettings> SalesSettings => Set<PlatformSalesSettings>();

    public DbSet<PlatformPromotion> Promotions => Set<PlatformPromotion>();

    public DbSet<PlatformOffer> Offers => Set<PlatformOffer>();

    public DbSet<Vixly.Platform.Domain.Billing.PlatformInvoice> Invoices => Set<Vixly.Platform.Domain.Billing.PlatformInvoice>();

    public DbSet<Vixly.Platform.Domain.Billing.PlatformInvoiceEvent> InvoiceEvents => Set<Vixly.Platform.Domain.Billing.PlatformInvoiceEvent>();

    public DbSet<Vixly.Platform.Domain.Billing.PlatformNumberSequence> NumberSequences => Set<Vixly.Platform.Domain.Billing.PlatformNumberSequence>();

    public DbSet<Vixly.Platform.Domain.Billing.PlatformTaxRate> TaxRates => Set<Vixly.Platform.Domain.Billing.PlatformTaxRate>();

    public DbSet<Vixly.Platform.Domain.Billing.PlatformBillingSettings> BillingSettings => Set<Vixly.Platform.Domain.Billing.PlatformBillingSettings>();

    public DbSet<Vixly.Platform.Domain.Billing.PlatformCompanyProfile> CompanyProfiles => Set<Vixly.Platform.Domain.Billing.PlatformCompanyProfile>();

    public DbSet<Vixly.Platform.Domain.Billing.PlatformBillingDocument> BillingDocuments => Set<Vixly.Platform.Domain.Billing.PlatformBillingDocument>();

    public DbSet<Vixly.Platform.Domain.Payments.PlatformMerchantAccount> MerchantAccounts => Set<Vixly.Platform.Domain.Payments.PlatformMerchantAccount>();

    public DbSet<Vixly.Platform.Domain.Payments.PlatformTenantCardProfile> TenantCardProfiles => Set<Vixly.Platform.Domain.Payments.PlatformTenantCardProfile>();

    public DbSet<Vixly.Platform.Domain.Payments.PlatformSavedCard> SavedCards => Set<Vixly.Platform.Domain.Payments.PlatformSavedCard>();

    public DbSet<Vixly.Platform.Domain.Payments.PlatformCardTransaction> CardTransactions => Set<Vixly.Platform.Domain.Payments.PlatformCardTransaction>();

    public DbSet<Vixly.Platform.Domain.Payments.PlatformPaymentLink> PaymentLinks => Set<Vixly.Platform.Domain.Payments.PlatformPaymentLink>();

    public DbSet<Vixly.Platform.Domain.Payments.PlatformProviderEvent> ProviderEvents => Set<Vixly.Platform.Domain.Payments.PlatformProviderEvent>();

    public DbSet<Vixly.Platform.Domain.Collections.PlatformCollectionsSettings> CollectionsSettings => Set<Vixly.Platform.Domain.Collections.PlatformCollectionsSettings>();

    public DbSet<Vixly.Platform.Domain.Collections.PlatformCollectionCase> CollectionCases => Set<Vixly.Platform.Domain.Collections.PlatformCollectionCase>();

    public DbSet<Vixly.Platform.Domain.Collections.PlatformCollectionCaseEvent> CollectionCaseEvents => Set<Vixly.Platform.Domain.Collections.PlatformCollectionCaseEvent>();

    public DbSet<Vixly.Platform.Domain.Licensing.PlatformTenantLicensing> TenantLicensing => Set<Vixly.Platform.Domain.Licensing.PlatformTenantLicensing>();

    public DbSet<Vixly.Platform.Domain.Licensing.PlatformDevice> Devices => Set<Vixly.Platform.Domain.Licensing.PlatformDevice>();

    public DbSet<Vixly.Platform.Domain.Licensing.PlatformDeviceChallenge> DeviceChallenges => Set<Vixly.Platform.Domain.Licensing.PlatformDeviceChallenge>();

    public DbSet<Vixly.Platform.Domain.Licensing.PlatformDeviceLink> DeviceLinks => Set<Vixly.Platform.Domain.Licensing.PlatformDeviceLink>();

    public DbSet<Vixly.Platform.Domain.Licensing.PlatformOwnerSupportRequest> OwnerSupportRequests => Set<Vixly.Platform.Domain.Licensing.PlatformOwnerSupportRequest>();

    public DbSet<Vixly.Platform.Domain.Licensing.PlatformDeviceEvent> DeviceEvents => Set<Vixly.Platform.Domain.Licensing.PlatformDeviceEvent>();

    public DbSet<Vixly.Platform.Domain.Payments.PlatformZelleAccount> ZelleAccounts => Set<Vixly.Platform.Domain.Payments.PlatformZelleAccount>();

    public DbSet<Vixly.Platform.Domain.Payments.PlatformZelleClaim> ZelleClaims => Set<Vixly.Platform.Domain.Payments.PlatformZelleClaim>();

    public DbSet<Vixly.Platform.Domain.Legal.PlatformLegalDocument> LegalDocuments => Set<Vixly.Platform.Domain.Legal.PlatformLegalDocument>();

    public DbSet<Vixly.Platform.Domain.Legal.PlatformLegalDocumentFile> LegalDocumentFiles => Set<Vixly.Platform.Domain.Legal.PlatformLegalDocumentFile>();

    public DbSet<Vixly.Platform.Domain.Legal.PlatformTenantTermsAcceptance> TenantTermsAcceptances => Set<Vixly.Platform.Domain.Legal.PlatformTenantTermsAcceptance>();

    public DbSet<Vixly.Platform.Domain.Legal.PlatformTermsReminder> TermsReminders => Set<Vixly.Platform.Domain.Legal.PlatformTermsReminder>();

    public DbSet<DataProtectionKey> DataProtectionKeys => Set<DataProtectionKey>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);

        modelBuilder.HasDefaultSchema(PlatformDatabase.Schema);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(PlatformDbContext).Assembly);

        ApplicationAssignedKeyConvention.Apply(modelBuilder);
    }
}
