using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using Vixly.Operations.Application.Audit;
using Vixly.Operations.Application.Devices;
using Vixly.Operations.Application.Identity;
using Vixly.Operations.Application.Messaging;
using Vixly.Operations.Application.Platform;
using Vixly.Operations.Application.Ports;
using Vixly.Operations.Application.Security;
using Vixly.Operations.Application.Setup;
using Vixly.Operations.Application.UseCases.Account.ChangeMyOperationsPassword;
using Vixly.Operations.Application.UseCases.Account.CompleteOperationsSecondFactorReset;
using Vixly.Operations.Application.UseCases.Account.ConfirmOperationsSecondFactor;
using Vixly.Operations.Application.UseCases.Account.EndMyOperationsSession;
using Vixly.Operations.Application.UseCases.Account.GetMyOperationsAccount;
using Vixly.Operations.Application.UseCases.Account.RegenerateMyRecoveryCodes;
using Vixly.Operations.Application.UseCases.Devices.CompleteOperationsDeviceLink;
using Vixly.Operations.Application.UseCases.Devices.DecideOperationsDevice;
using Vixly.Operations.Application.UseCases.Devices.EnrolOperationsDevice;
using Vixly.Operations.Application.UseCases.Devices.ListOperationsDevices;
using Vixly.Operations.Application.UseCases.Devices.ReleaseOperationsDevice;
using Vixly.Operations.Application.UseCases.Devices.RequestOperationsFirstDeviceLink;
using Vixly.Operations.Application.UseCases.Devices.VerifyOperationsDevice;
using Vixly.Operations.Application.UseCases.Legal.AcceptOperationsLegalTerms;
using Vixly.Operations.Application.UseCases.Legal.GetOperationsLegalStatus;
using Vixly.Operations.Application.UseCases.Locations.ListOperationsLocations;
using Vixly.Operations.Application.UseCases.Locations.RemoveOperationsCountryContact;
using Vixly.Operations.Application.UseCases.Locations.SaveOperationsBranch;
using Vixly.Operations.Application.UseCases.Locations.SaveOperationsCountryContact;
using Vixly.Operations.Application.UseCases.Locations.SaveOperationsWarehouse;
using Vixly.Operations.Application.UseCases.Roles.GrantOperationsRole;
using Vixly.Operations.Application.UseCases.Roles.ListOperationsRoles;
using Vixly.Operations.Application.UseCases.Roles.RevokeOperationsRoleGrant;
using Vixly.Operations.Application.UseCases.Roles.SaveOperationsRole;
using Vixly.Operations.Application.UseCases.Roles.SetOperationsRoleActive;
using Vixly.Operations.Application.UseCases.SignIn.AcceptOperationsInvitation;
using Vixly.Operations.Application.UseCases.SignIn.BeginOperationsSignIn;
using Vixly.Operations.Application.UseCases.SignIn.BeginOperationsSignInEnrolment;
using Vixly.Operations.Application.UseCases.SignIn.CompleteOperationsSignInEnrolment;
using Vixly.Operations.Application.UseCases.SignIn.CompleteOperationsSignInWithAuthenticatorCode;
using Vixly.Operations.Application.UseCases.SignIn.CompleteOperationsSignInWithRecoveryCode;
using Vixly.Operations.Application.UseCases.SignIn.GetOperationsSignInBrand;
using Vixly.Operations.Application.UseCases.SignIn.RequestOperationsPasswordReset;
using Vixly.Operations.Application.UseCases.SignIn.ResetOperationsPassword;
using Vixly.Operations.Application.UseCases.SignIn.SignOutOperations;
using Vixly.Operations.Application.UseCases.Site.GetOperationsPublicSite;
using Vixly.Operations.Application.UseCases.Site.GetOperationsSiteDraft;
using Vixly.Operations.Application.UseCases.Site.GetOperationsSiteImage;
using Vixly.Operations.Application.UseCases.Site.PublishOperationsSite;
using Vixly.Operations.Application.UseCases.Site.RemoveOperationsSiteImage;
using Vixly.Operations.Application.UseCases.Site.TrackOperationsShipmentPublicly;
using Vixly.Operations.Application.UseCases.Site.UpdateOperationsSiteDraft;
using Vixly.Operations.Application.UseCases.Site.UploadOperationsSiteImage;
using Vixly.Operations.Application.UseCases.Staff.ChangeOperationsStaffStatus;
using Vixly.Operations.Application.UseCases.Staff.EndOperationsStaffSessions;
using Vixly.Operations.Application.UseCases.Staff.GetOperationsStaffMember;
using Vixly.Operations.Application.UseCases.Staff.InviteOperationsStaffMember;
using Vixly.Operations.Application.UseCases.Staff.ListOperationsStaff;
using Vixly.Operations.Application.UseCases.Staff.ResetOperationsStaffSecondFactor;
using Vixly.Operations.Application.UseCases.Staff.TransferOperationsOwnership;
using Vixly.Operations.Application.UseCases.Tenancy.ResolveOperationsTenant;
using Vixly.Operations.Infrastructure.Mail;
using Vixly.Operations.Infrastructure.Persistence.Repositories;
using Vixly.Operations.Infrastructure.Persistence;
using Vixly.Operations.Infrastructure.Platform;
using Vixly.Operations.Infrastructure.Security;

namespace Vixly.Operations.Infrastructure.DependencyInjection;

// =========================================================
// FILE: OperationsInfrastructureServiceCollectionExtensions.cs
//
// PURPOSE:
// Registers everything Operations needs from Infrastructure (composition root only).
//
// RESPONSIBILITIES:
// - Register the DbContext, tenant scope, unit of work and repositories
// - Register the Platform client, signer and tenant directory
// - Register the Slice 0 use cases, outbox and feed clock
// - Persist the Data Protection key ring in ops (S-5)
//
// IMPORTANT NOTES:
// - Each host passes its own Data Protection application name
// - A missing connection string stops the start
// =========================================================
public static class OperationsInfrastructureServiceCollectionExtensions
{
    public static IServiceCollection AddOperationsInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration,
        string dataProtectionApplicationName)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentException.ThrowIfNullOrWhiteSpace(dataProtectionApplicationName);

        string? connectionString = configuration.GetConnectionString(OperationsDatabase.ConnectionStringName);

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException($"ConnectionStrings:{OperationsDatabase.ConnectionStringName} is not configured.");
        }

        services.AddScoped<OperationsTenantScope>();
        services.AddScoped<IOperationsTenantScope>(provider => provider.GetRequiredService<OperationsTenantScope>());
        services.AddDbContext<OperationsDbContext>(options => options.UseSqlServer(
            connectionString,
            sql => sql
                .MigrationsHistoryTable(OperationsDatabase.MigrationsHistoryTable, OperationsDatabase.Schema)
                .UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery)
                .EnableRetryOnFailure()));

        services.AddDataProtection()
            .SetApplicationName(dataProtectionApplicationName)
            .PersistKeysToDbContext<OperationsDbContext>();

        services.TryAddTimeProvider();
        services.AddMemoryCache();
        services.AddScoped<IOperationsUnitOfWork, OperationsUnitOfWork>();
        services.AddScoped<IOperationsOutboxRepository, OperationsOutboxRepository>();
        services.AddScoped<IOperationsJobRunRepository, OperationsJobRunRepository>();
        services.AddScoped<IOperationsSignInCleanupRepository, OperationsSignInCleanupRepository>();
        services.AddScoped<Vixly.Operations.Application.Identity.OperationsSignInCleanup>();
        services.AddScoped<Vixly.Operations.Application.Ports.IOperationsTrackingNumberBackfill, Vixly.Operations.Infrastructure.Persistence.OperationsTrackingNumberBackfill>();
        services.AddScoped<IOperationsFeedPositionRepository, OperationsFeedPositionRepository>();
        services.AddScoped<IOperationsUserRepository, OperationsUserRepository>();
        services.AddScoped<IOperationsSessionRepository, OperationsSessionRepository>();
        services.AddScoped<IOperationsDeviceBlockRepository, OperationsDeviceBlockRepository>();
        services.AddScoped<IOperationsQuickSignInRepository, OperationsQuickSignInRepository>();
        services.AddScoped<IOperationsDriverTrackingRepository, OperationsDriverTrackingRepository>();
        services.AddScoped<IOperationsShelfCountRepository, OperationsShelfCountRepository>();
        services.AddScoped<Vixly.Operations.Application.UseCases.Printing.CreateOperationsPrintPairing.CreateOperationsPrintPairingUseCase>();
        services.AddScoped<Vixly.Operations.Application.UseCases.Printing.PairOperationsPrintAgent.PairOperationsPrintAgentUseCase>();
        services.AddScoped<Vixly.Operations.Application.UseCases.Printing.SaveOperationsPhonePrinter.SaveOperationsPhonePrinterUseCase>();
        services.AddScoped<Vixly.Operations.Application.UseCases.Receiving.StartOperationsShelfCount.StartOperationsShelfCountUseCase>();
        services.AddScoped<Vixly.Operations.Application.UseCases.Receiving.ScanOperationsShelfCount.ScanOperationsShelfCountUseCase>();
        services.AddScoped<Vixly.Operations.Application.UseCases.Receiving.FinishOperationsShelfCount.FinishOperationsShelfCountUseCase>();
        services.AddScoped<Vixly.Operations.Application.UseCases.Receiving.GetOperationsShelfCounts.GetOperationsShelfCountsUseCase>();
        services.AddScoped<Vixly.Operations.Application.Delivery.OperationsDriverTracking>();
        services.AddScoped<Vixly.Operations.Application.UseCases.Delivery.RecordOperationsDriverPosition.RecordOperationsDriverPositionUseCase>();
        services.AddScoped<Vixly.Operations.Application.UseCases.Delivery.GetOperationsRouteMap.GetOperationsRouteMapUseCase>();
        services.AddScoped<Vixly.Operations.Application.UseCases.Customers.GetMyOperationsShipmentDriver.GetMyOperationsShipmentDriverUseCase>();
        services.AddScoped<Vixly.Operations.Application.UseCases.Customers.GetOperationsTrackedDelivery.GetOperationsTrackedDeliveryUseCase>();
        services.AddOptions<Vixly.Operations.Infrastructure.Maps.OperationsMapOptions>().Bind(configuration.GetSection(Vixly.Operations.Infrastructure.Maps.OperationsMapOptions.SectionName));
        services.AddSingleton<Vixly.Operations.Infrastructure.Maps.OperationsMapTiles>();
        services.AddHttpClient(Vixly.Operations.Infrastructure.Maps.OperationsMapTiles.HttpClientName, client => client.Timeout = TimeSpan.FromSeconds(10));
        services.AddScoped<Vixly.Operations.Application.Identity.OperationsDeviceBlocking>();
        services.AddScoped<IOperationsSignInChallengeRepository, OperationsSignInChallengeRepository>();
        services.AddScoped<IOperationsAccountTokenRepository, OperationsAccountTokenRepository>();
        services.AddScoped<OperationsAuditRepository>();
        services.AddScoped<IOperationsAuditRepository>(provider => provider.GetRequiredService<OperationsAuditRepository>());
        services.AddScoped<IOperationsAuditReader>(provider => provider.GetRequiredService<OperationsAuditRepository>());
        services.AddScoped<IOperationsBrandRepository, OperationsBrandRepository>();
        services.AddScoped<IOperationsRoleRepository, OperationsRoleRepository>();
        services.AddScoped<IOperationsSettingsRepository, OperationsSettingsRepository>();
        services.AddScoped<IOperationsIdleTimesReader, Vixly.Operations.Infrastructure.Persistence.OperationsIdleTimesReader>();
        services.AddScoped<IOperationsShippingServiceRepository, OperationsShippingServiceRepository>();
        services.AddScoped<IOperationsCustomerRepository, OperationsCustomerRepository>();
        services.AddScoped<IOperationsTrackingRepository, OperationsTrackingRepository>();
        services.AddScoped<Vixly.Operations.Application.Tracking.OperationsTracker>();
        services.AddScoped<IOperationsNumberAllocator, OperationsNumberAllocator>();
        services.AddScoped<Vixly.Operations.Application.Settings.OperationsNumbering>();

        // Staff identity (Slice 1): own security code (ADR-0020 rejected).
        services.AddLocalization();
        services.AddSingleton<IOperationsPasswordHasher, BCryptOperationsPasswordHasher>();
        services.AddSingleton<IOperationsSecretGenerator, OperationsSecretGenerator>();
        services.AddSingleton<IOperationsAuthenticatorCodes, TotpOperationsAuthenticatorCodes>();
        services.TryAddScoped<IOperationsRequestMetadata, OperationsNoRequestMetadata>();
        services.AddOptions<OperationsMailOptions>().Bind(configuration.GetSection(OperationsMailOptions.SectionName));
        // Scoped: the company's email look comes from the tenant scope (owner, 2026-10-25).
        services.AddScoped<OperationsMailBrandReader>();
        services.AddScoped<OperationsMailSender>();
        services.AddScoped<IOperationsIdentityMailer, SmtpOperationsIdentityMailer>();
        services.AddScoped<IOperationsCustomerMailer, SmtpOperationsCustomerMailer>();
        services.AddOptions<OperationsLinkOptions>().Bind(configuration.GetSection(OperationsLinkOptions.SectionName));
        services.AddSingleton<IOperationsPortalLinks, OperationsPortalLinks>();
        services.AddScoped<OperationsAuditTrail>();
        services.AddScoped<OperationsSignInSteps>();
        services.AddScoped<OperationsSessionValidator>();
        services.AddScoped<BeginOperationsSignInUseCase>();
        services.AddScoped<CompleteOperationsSignInWithAuthenticatorCodeUseCase>();
        services.AddScoped<Vixly.Operations.Application.UseCases.SignIn.BeginOperationsQuickSignIn.BeginOperationsQuickSignInUseCase>();
        services.AddScoped<Vixly.Operations.Application.UseCases.SignIn.CompleteOperationsSignInWithPassword.CompleteOperationsSignInWithPasswordUseCase>();
        services.AddScoped<Vixly.Operations.Application.UseCases.SignIn.ListOperationsDevicePeople.ListOperationsDevicePeopleUseCase>();
        services.AddScoped<Vixly.Operations.Application.UseCases.Account.SetMyOperationsPin.SetMyOperationsPinUseCase>();
        services.AddScoped<Vixly.Operations.Application.UseCases.Staff.IssueOperationsStaffBadge.IssueOperationsStaffBadgeUseCase>();
        services.AddScoped<Vixly.Operations.Application.UseCases.Staff.RevokeOperationsStaffBadge.RevokeOperationsStaffBadgeUseCase>();
        services.AddScoped<Vixly.Operations.Application.UseCases.Staff.GetOperationsQuickSignInState.GetOperationsQuickSignInStateUseCase>();
        services.AddScoped<CompleteOperationsSignInWithRecoveryCodeUseCase>();
        services.AddScoped<BeginOperationsSignInEnrolmentUseCase>();
        services.AddScoped<CompleteOperationsSignInEnrolmentUseCase>();
        services.AddScoped<AcceptOperationsInvitationUseCase>();
        services.AddScoped<SignOutOperationsUseCase>();
        services.AddScoped<GetOperationsSignInBrandUseCase>();
        services.AddScoped<IOperationsOutboxHandler, OperationsOwnerInvitationHandler>();

        // Onboarding completion (Slice 1C).
        services.AddSingleton<OperationsPlatformSender>();
        services.AddSingleton<IOperationsDeviceClient, OperationsDeviceClient>();
        services.AddSingleton<IOperationsLegalClient, OperationsLegalClient>();
        services.AddSingleton<IOperationsCompanyClient, OperationsCompanyClient>();
        // P-4: kept answers from Platform are served while one background call refreshes them.
        services.AddSingleton<OperationsStaleWhileRefresh>();
        services.AddSingleton<IOperationsPlanFeatures, OperationsPlanFeaturesClient>();
        services.AddSingleton<IOperationsUsageReporter, OperationsUsageReporter>();
        services.AddScoped<CompleteOperationsSecondFactorResetUseCase>();
        services.AddScoped<GetOperationsLegalStatusUseCase>();
        services.AddScoped<AcceptOperationsLegalTermsUseCase>();
        services.AddScoped<OperationsActors>();
        services.AddScoped<OperationsStepUp>();
        services.AddScoped<RequestOperationsFirstDeviceLinkUseCase>();
        services.AddScoped<CompleteOperationsDeviceLinkUseCase>();
        services.AddScoped<EnrolOperationsDeviceUseCase>();
        services.AddScoped<VerifyOperationsDeviceUseCase>();
        services.AddScoped<ListOperationsDevicesUseCase>();
        services.AddScoped<DecideOperationsDeviceUseCase>();
        services.AddScoped<ReleaseOperationsDeviceUseCase>();
        services.AddScoped<Vixly.Operations.Application.UseCases.Devices.UnblockOperationsDevice.UnblockOperationsDeviceUseCase>();
        services.AddScoped<RequestOperationsPasswordResetUseCase>();
        services.AddScoped<ResetOperationsPasswordUseCase>();

        // Tenant public site (Slice 1B).
        services.AddScoped<IOperationsSiteRepository, OperationsSiteRepository>();
        services.AddScoped<IOperationsItemRepository, OperationsItemRepository>();
        services.AddSingleton<IOperationsItemStarterList, Vixly.Operations.Infrastructure.Items.OperationsItemStarterList>();
        services.AddScoped<IOperationsLocationRepository, OperationsLocationRepository>();
        services.AddSingleton<IOperationsImageInspector, OperationsImageInspector>();
        services.AddScoped<OperationsAccess>();

        // Staff, roles and my account (Slice 1D).
        services.AddScoped<Vixly.Operations.Application.Staff.OperationsStaffDirectory>();
        services.AddScoped<ChangeOperationsStaffStatusUseCase>();
        services.AddScoped<EndOperationsStaffSessionsUseCase>();
        services.AddScoped<GetOperationsStaffMemberUseCase>();
        services.AddScoped<InviteOperationsStaffMemberUseCase>();
        services.AddScoped<ListOperationsStaffUseCase>();
        services.AddScoped<Vixly.Operations.Application.UseCases.Staff.GetOperationsMenu.GetOperationsMenuUseCase>();
        services.AddScoped<ResetOperationsStaffSecondFactorUseCase>();
        services.AddScoped<TransferOperationsOwnershipUseCase>();
        services.AddScoped<GrantOperationsRoleUseCase>();
        services.AddScoped<ListOperationsRolesUseCase>();
        // Owner, 2026-10-29: the standard roles every company starts with.
        services.AddScoped<IOperationsRoleNames, Vixly.Operations.Infrastructure.Identity.OperationsRoleNames>();
        services.AddScoped<Vixly.Operations.Application.Security.OperationsStandardRoleSetup>();
        services.AddScoped<RevokeOperationsRoleGrantUseCase>();
        services.AddScoped<SaveOperationsRoleUseCase>();
        services.AddScoped<SetOperationsRoleActiveUseCase>();

        // Customers (Slice 2).
        services.AddScoped<Vixly.Operations.Application.Customers.OperationsCustomerAccounts>();
        services.AddScoped<Vixly.Operations.Application.UseCases.Customers.GetOperationsCustomerPortalBrand.GetOperationsCustomerPortalBrandUseCase>();
        services.AddScoped<Vixly.Operations.Application.UseCases.Customers.SignUpOperationsCustomer.SignUpOperationsCustomerUseCase>();
        services.AddScoped<Vixly.Operations.Application.UseCases.Customers.VerifyOperationsCustomerEmail.VerifyOperationsCustomerEmailUseCase>();
        services.AddScoped<Vixly.Operations.Application.UseCases.Customers.ResendOperationsCustomerVerification.ResendOperationsCustomerVerificationUseCase>();
        services.AddScoped<Vixly.Operations.Application.UseCases.Customers.ResendOperationsCustomerConfirmation.ResendOperationsCustomerConfirmationUseCase>();
        services.AddScoped<Vixly.Operations.Application.UseCases.Customers.SignInOperationsCustomer.SignInOperationsCustomerUseCase>();
        services.AddScoped<Vixly.Operations.Application.UseCases.Customers.SignOutOperationsCustomer.SignOutOperationsCustomerUseCase>();
        services.AddScoped<Vixly.Operations.Application.UseCases.Customers.RequestOperationsCustomerPasswordReset.RequestOperationsCustomerPasswordResetUseCase>();
        services.AddScoped<Vixly.Operations.Application.UseCases.Customers.ResetOperationsCustomerPassword.ResetOperationsCustomerPasswordUseCase>();
        services.AddScoped<Vixly.Operations.Application.UseCases.Customers.GetMyOperationsCustomer.GetMyOperationsCustomerUseCase>();
        services.AddScoped<Vixly.Operations.Application.UseCases.Customers.UpdateMyOperationsCustomer.UpdateMyOperationsCustomerUseCase>();
        services.AddScoped<Vixly.Operations.Application.UseCases.Customers.ChangeMyOperationsCustomerAddress.ChangeMyOperationsCustomerAddressUseCase>();
        services.AddScoped<Vixly.Operations.Application.UseCases.Customers.DismissMyOperationsCustomerTour.DismissMyOperationsCustomerTourUseCase>();
        services.AddScoped<Vixly.Operations.Application.UseCases.Cashier.ChangeOperationsFreightCollect.ChangeOperationsFreightCollectUseCase>();
        services.AddScoped<Vixly.Operations.Application.UseCases.Customers.ChangeMyOperationsCustomerPassword.ChangeMyOperationsCustomerPasswordUseCase>();
        services.AddScoped<Vixly.Operations.Application.UseCases.Customers.ListOperationsCustomers.ListOperationsCustomersUseCase>();
        // Owner, 2026-10-29 (design 4.4): find one customer, recorded and limited; the list stays with the company.
        services.AddScoped<Vixly.Operations.Application.Customers.OperationsCustomerLookups>();
        services.AddScoped<Vixly.Operations.Application.UseCases.Customers.LookUpOperationsCustomers.LookUpOperationsCustomersUseCase>();
        services.AddScoped<Vixly.Operations.Application.UseCases.Customers.CreateOperationsCustomer.CreateOperationsCustomerUseCase>();

        // Owner, 2026-10-29 (design section 9): the support agent's call, the call tickets, outside accounts.
        services.AddScoped<Vixly.Operations.Application.Support.OperationsSupportDesk>();
        services.AddScoped<Vixly.Operations.Application.UseCases.Support.FindOperationsSupportCaller.FindOperationsSupportCallerUseCase>();
        services.AddScoped<Vixly.Operations.Application.UseCases.Support.GetOperationsSupportCase.GetOperationsSupportCaseUseCase>();
        services.AddScoped<Vixly.Operations.Application.UseCases.Support.OpenOperationsSupportRequest.OpenOperationsSupportRequestUseCase>();
        services.AddScoped<Vixly.Operations.Application.UseCases.Staff.SetOperationsStaffOutside.SetOperationsStaffOutsideUseCase>();
        services.AddScoped<Vixly.Operations.Application.UseCases.Customers.GetOperationsCustomer.GetOperationsCustomerUseCase>();
        services.AddScoped<Vixly.Operations.Application.UseCases.Customers.ChangeOperationsCustomerStatus.ChangeOperationsCustomerStatusUseCase>();

        // Receiving (Slice 3B).
        services.AddScoped<IOperationsReceivingRepository, OperationsReceivingRepository>();
        services.AddScoped<IOperationsInspectionRepository, OperationsInspectionRepository>();
        services.AddScoped<IOperationsHoldRepository, OperationsHoldRepository>();
        services.AddScoped<IOperationsStockCountRepository, OperationsStockCountRepository>();
        services.AddScoped<IOperationsTicketRepository, OperationsTicketRepository>();
        services.AddScoped<IOperationsShipmentCounter, OperationsShipmentCounter>();
        services.AddScoped<IOperationsInsuranceRepository, OperationsInsuranceRepository>();
        services.AddScoped<IOperationsChargeRepository, OperationsChargeRepository>();
        services.AddScoped<IOperationsSupplyRepository, OperationsSupplyRepository>();
        services.AddScoped<IOperationsBillingRepository, OperationsBillingRepository>();
        services.AddScoped<IOperationsPaymentRepository, OperationsPaymentRepository>();
        services.AddScoped<IOperationsBundleRepository, OperationsBundleRepository>();
        services.AddScoped<IOperationsDispatchRepository, OperationsDispatchRepository>();
        services.AddScoped<IOperationsEventRepository, OperationsEventRepository>();
        services.AddScoped<IOperationsArrivalRepository, OperationsArrivalRepository>();
        services.AddScoped<IOperationsPickupRepository, OperationsPickupRepository>();
        services.AddScoped<IOperationsDeliveryRepository, OperationsDeliveryRepository>();
        services.AddScoped<IOperationsCreditRepository, OperationsCreditRepository>();
        services.AddScoped<Vixly.Operations.Application.Holds.OperationsUnclaimedReminders>();
        // Owner, 2026-10-28: company brands for the Cloudflare Worker's maintenance page.
        services.AddOptions<Vixly.Operations.Infrastructure.Edge.OperationsEdgeOptions>().Bind(configuration.GetSection(Vixly.Operations.Infrastructure.Edge.OperationsEdgeOptions.SectionName));
        services.AddHttpClient<IOperationsEdgeClient, Vixly.Operations.Infrastructure.Edge.OperationsEdgeClient>(client => client.Timeout = TimeSpan.FromSeconds(15));
        services.AddScoped<Vixly.Operations.Application.Branding.OperationsEdgeBrands>();
        services.AddScoped<IOperationsOutboxHandler, Vixly.Operations.Application.Branding.OperationsEdgeBrandHandler>();
        services.AddScoped<Vixly.Operations.Application.UseCases.Holds.AnswerMyOperationsHold.AnswerMyOperationsHoldUseCase>();
        services.AddScoped<Vixly.Operations.Application.UseCases.Holds.ReviewOperationsHoldAnswer.ReviewOperationsHoldAnswerUseCase>();
        services.AddScoped<Vixly.Operations.Application.UseCases.Holds.GetOperationsHoldAnswerDocument.GetOperationsHoldAnswerDocumentUseCase>();
        services.AddScoped<Vixly.Operations.Application.UseCases.Holds.DecideOperationsUnclaimed.DecideOperationsUnclaimedUseCase>();

        // Slice 4: the charges engine.
        services.AddScoped<Vixly.Operations.Application.Charges.OperationsRater>();
        services.AddScoped<Vixly.Operations.Application.UseCases.Charges.GetOperationsChargesSetup.GetOperationsChargesSetupUseCase>();
        services.AddScoped<Vixly.Operations.Application.UseCases.Charges.SaveOperationsPriceList.SaveOperationsPriceListUseCase>();
        services.AddScoped<Vixly.Operations.Application.UseCases.Charges.RemoveOperationsScheduledVersion.RemoveOperationsScheduledVersionUseCase>();
        services.AddScoped<Vixly.Operations.Application.UseCases.Charges.SaveOperationsItemType.SaveOperationsItemTypeUseCase>();
        // Slice 5E: the supplies catalogue.
        services.AddScoped<Vixly.Operations.Application.UseCases.Supplies.ListOperationsSupplies.ListOperationsSuppliesUseCase>();
        services.AddScoped<Vixly.Operations.Application.UseCases.Supplies.GetOperationsSupply.GetOperationsSupplyUseCase>();
        services.AddScoped<Vixly.Operations.Application.UseCases.Supplies.SaveOperationsSupply.SaveOperationsSupplyUseCase>();
        services.AddScoped<Vixly.Operations.Application.UseCases.Supplies.ScheduleOperationsSupplyPrice.ScheduleOperationsSupplyPriceUseCase>();
        services.AddScoped<Vixly.Operations.Application.UseCases.Supplies.RemoveOperationsSupplyPrice.RemoveOperationsSupplyPriceUseCase>();
        services.AddScoped<Vixly.Operations.Application.UseCases.Supplies.GetOperationsSupplyLabels.GetOperationsSupplyLabelsUseCase>();
        // Slice 5E step 2: the supplies stock.
        services.AddScoped<Vixly.Operations.Application.Supplies.OperationsSupplyDesk>();
        services.AddScoped<Vixly.Operations.Application.Supplies.OperationsSupplyStockKeeper>();
        services.AddScoped<Vixly.Operations.Application.Supplies.OperationsSupplySales>();
        services.AddScoped<Vixly.Operations.Application.Billing.OperationsInvoicing>();
        services.AddScoped<Vixly.Operations.Application.Supplies.OperationsSupplyDiscounts>();
        services.AddScoped<Vixly.Operations.Application.Supplies.OperationsSupplyCounter>();
        services.AddScoped<Vixly.Operations.Application.UseCases.Counter.AddOperationsCounterSupply.AddOperationsCounterSupplyUseCase>();
        services.AddScoped<Vixly.Operations.Application.UseCases.Counter.ChangeOperationsCounterSupplyLine.ChangeOperationsCounterSupplyLineUseCase>();
        services.AddScoped<Vixly.Operations.Application.UseCases.Counter.StartOperationsSupplyOrder.StartOperationsSupplyOrderUseCase>();
        services.AddScoped<Vixly.Operations.Application.UseCases.Counter.GetOperationsSupplyOrder.GetOperationsSupplyOrderUseCase>();
        services.AddScoped<Vixly.Operations.Application.UseCases.Counter.ConfirmOperationsSupplyOrder.ConfirmOperationsSupplyOrderUseCase>();
        // Slice 5F step 1: invoices and credit notes.
        services.AddScoped<Vixly.Operations.Application.Billing.OperationsBillingDesk>();
        // Slice 5F step 2b: credit accounts.
        services.AddScoped<Vixly.Operations.Application.Billing.OperationsCreditBook>();
        services.AddScoped<Vixly.Operations.Application.Billing.OperationsCreditStatements>();
        services.AddScoped<Vixly.Operations.Application.Billing.OperationsStatementNotices>();
        services.AddScoped<Vixly.Operations.Application.UseCases.Credit.ListOperationsCreditAccounts.ListOperationsCreditAccountsUseCase>();
        services.AddScoped<Vixly.Operations.Application.UseCases.Credit.GetOperationsCreditAccount.GetOperationsCreditAccountUseCase>();
        services.AddScoped<Vixly.Operations.Application.UseCases.Credit.OpenOperationsCreditAccount.OpenOperationsCreditAccountUseCase>();
        services.AddScoped<Vixly.Operations.Application.UseCases.Credit.ChangeOperationsCreditAccount.ChangeOperationsCreditAccountUseCase>();
        services.AddScoped<Vixly.Operations.Application.UseCases.Credit.ListOperationsCreditCharges.ListOperationsCreditChargesUseCase>();
        services.AddScoped<Vixly.Operations.Application.UseCases.Credit.DecideOperationsCreditCharge.DecideOperationsCreditChargeUseCase>();
        services.AddScoped<Vixly.Operations.Application.UseCases.Cashier.BillOperationsAccount.BillOperationsAccountUseCase>();
        services.AddScoped<Vixly.Operations.Application.UseCases.Credit.GetOperationsCreditStatement.GetOperationsCreditStatementUseCase>();
        services.AddScoped<Vixly.Operations.Application.UseCases.Credit.GetOperationsCreditSettings.GetOperationsCreditSettingsUseCase>();
        services.AddScoped<Vixly.Operations.Application.UseCases.Credit.SaveOperationsCreditSettings.SaveOperationsCreditSettingsUseCase>();
        services.AddScoped<Vixly.Operations.Application.UseCases.Billing.GetMyOperationsCreditAccount.GetMyOperationsCreditAccountUseCase>();
        services.AddScoped<Vixly.Operations.Application.UseCases.Billing.GetMyOperationsCreditStatement.GetMyOperationsCreditStatementUseCase>();
        // Slice 6A: bundles and repack boxes.
        services.AddScoped<Vixly.Operations.Application.Bundles.OperationsBundleDesk>();
        services.AddScoped<Vixly.Operations.Application.Bundles.OperationsBundling>();
        services.AddScoped<Vixly.Operations.Application.Bundles.OperationsBundleShipments>();
        services.AddScoped<Vixly.Operations.Application.UseCases.Bundles.GetOperationsBundles.GetOperationsBundlesUseCase>();
        services.AddScoped<Vixly.Operations.Application.UseCases.Bundles.CreateOperationsBundle.CreateOperationsBundleUseCase>();
        services.AddScoped<Vixly.Operations.Application.UseCases.Bundles.GetOperationsBundle.GetOperationsBundleUseCase>();
        services.AddScoped<Vixly.Operations.Application.UseCases.Bundles.FindOperationsBundleCode.FindOperationsBundleCodeUseCase>();
        services.AddScoped<Vixly.Operations.Application.UseCases.Bundles.ScanIntoOperationsBundle.ScanIntoOperationsBundleUseCase>();
        services.AddScoped<Vixly.Operations.Application.UseCases.Bundles.TakeOutOfOperationsBundle.TakeOutOfOperationsBundleUseCase>();
        services.AddScoped<Vixly.Operations.Application.UseCases.Bundles.CloseOperationsBundle.CloseOperationsBundleUseCase>();
        services.AddScoped<Vixly.Operations.Application.UseCases.Bundles.VoidOperationsBundle.VoidOperationsBundleUseCase>();
        services.AddScoped<Vixly.Operations.Application.UseCases.Bundles.GetOperationsBundleLabel.GetOperationsBundleLabelUseCase>();
        services.AddScoped<Vixly.Operations.Application.UseCases.Bundles.GetOperationsRepackBoxes.GetOperationsRepackBoxesUseCase>();
        services.AddScoped<Vixly.Operations.Application.UseCases.Bundles.ChooseOperationsConsolidation.ChooseOperationsConsolidationUseCase>();
        services.AddScoped<Vixly.Operations.Application.UseCases.Bundles.OpenOperationsRepackBox.OpenOperationsRepackBoxUseCase>();
        services.AddScoped<Vixly.Operations.Application.UseCases.Bundles.GetOperationsRepackBox.GetOperationsRepackBoxUseCase>();
        services.AddScoped<Vixly.Operations.Application.UseCases.Bundles.ScanIntoOperationsRepackBox.ScanIntoOperationsRepackBoxUseCase>();
        services.AddScoped<Vixly.Operations.Application.UseCases.Bundles.TakeOutOfOperationsRepackBox.TakeOutOfOperationsRepackBoxUseCase>();
        services.AddScoped<Vixly.Operations.Application.UseCases.Bundles.CloseOperationsRepackBox.CloseOperationsRepackBoxUseCase>();
        services.AddScoped<Vixly.Operations.Application.UseCases.Bundles.VoidOperationsRepackBox.VoidOperationsRepackBoxUseCase>();
        // The bundle's list from the computer (owner, 2026-10-21).
        services.AddScoped<Vixly.Operations.Application.Bundles.OperationsBundleListing>();
        // Pricing pages (owner, 2026-10-21).
        services.AddScoped<Vixly.Operations.Application.Charges.OperationsPricing>();
        services.AddScoped<Vixly.Operations.Application.UseCases.Charges.GetMyOperationsPricing.GetMyOperationsPricingUseCase>();
        services.AddScoped<Vixly.Operations.Application.UseCases.Site.GetOperationsPublicPricing.GetOperationsPublicPricingUseCase>();
        services.AddScoped<Vixly.Operations.Application.UseCases.Site.EstimateOperationsPublicShipment.EstimateOperationsPublicShipmentUseCase>();
        services.AddScoped<Vixly.Operations.Application.UseCases.Site.ListOperationsSiteTestimonials.ListOperationsSiteTestimonialsUseCase>();
        services.AddScoped<Vixly.Operations.Application.UseCases.Site.RecordOperationsSiteTestimonial.RecordOperationsSiteTestimonialUseCase>();
        services.AddScoped<Vixly.Operations.Application.UseCases.Site.ChangeOperationsSiteTestimonial.ChangeOperationsSiteTestimonialUseCase>();
        // Customer reviews (owner, 2026-11-03).
        services.AddScoped<Vixly.Operations.Application.Site.OperationsCustomerReviews>();
        services.AddScoped<Vixly.Operations.Application.UseCases.Site.ChangeOperationsReviewInvitations.ChangeOperationsReviewInvitationsUseCase>();
        services.AddScoped<Vixly.Operations.Application.UseCases.Site.OpenOperationsReviewTicket.OpenOperationsReviewTicketUseCase>();
        services.AddScoped<Vixly.Operations.Application.UseCases.Site.GetMyOperationsReview.GetMyOperationsReviewUseCase>();
        services.AddScoped<Vixly.Operations.Application.UseCases.Site.SaveMyOperationsReview.SaveMyOperationsReviewUseCase>();
        services.AddScoped<Vixly.Operations.Application.UseCases.Site.DeleteMyOperationsReview.DeleteMyOperationsReviewUseCase>();
        services.AddScoped<Vixly.Operations.Application.UseCases.Site.DismissMyOperationsReviewInvite.DismissMyOperationsReviewInviteUseCase>();
        services.AddScoped<Vixly.Operations.Application.UseCases.Bundles.AddToOperationsBundleList.AddToOperationsBundleListUseCase>();
        services.AddScoped<Vixly.Operations.Application.UseCases.Bundles.RemoveFromOperationsBundleList.RemoveFromOperationsBundleListUseCase>();
        services.AddScoped<Vixly.Operations.Application.UseCases.Bundles.GetOperationsRepackBoxLabel.GetOperationsRepackBoxLabelUseCase>();
        services.AddScoped<Vixly.Operations.Application.UseCases.Bundles.GetOperationsBundleSettings.GetOperationsBundleSettingsUseCase>();
        services.AddScoped<Vixly.Operations.Application.UseCases.Bundles.SaveOperationsBundleSettings.SaveOperationsBundleSettingsUseCase>();
        // Slice 7 step 2: the pickup counter.
        services.AddScoped<Vixly.Operations.Application.Pickup.OperationsPickupDesk>();
        services.AddScoped<Vixly.Operations.Application.Pickup.OperationsPickupReminders>();
        services.AddScoped<Vixly.Operations.Application.Pickup.OperationsPickupCodes>();
        services.AddSingleton<IOperationsSecretProtector, Vixly.Operations.Infrastructure.Security.OperationsSecretProtector>();
        services.AddScoped<Vixly.Operations.Application.UseCases.Pickup.GetOperationsPickups.GetOperationsPickupsUseCase>();
        services.AddScoped<Vixly.Operations.Application.UseCases.Pickup.GetOperationsPickup.GetOperationsPickupUseCase>();
        services.AddScoped<Vixly.Operations.Application.UseCases.Pickup.BillOperationsPickup.BillOperationsPickupUseCase>();
        services.AddScoped<Vixly.Operations.Application.UseCases.Pickup.SendOperationsPickupCode.SendOperationsPickupCodeUseCase>();
        services.AddScoped<Vixly.Operations.Application.UseCases.Pickup.ReleaseOperationsPickup.ReleaseOperationsPickupUseCase>();
        services.AddScoped<Vixly.Operations.Application.UseCases.Pickup.GetOperationsPickupSettings.GetOperationsPickupSettingsUseCase>();
        services.AddScoped<Vixly.Operations.Application.UseCases.Pickup.SaveOperationsPickupSettings.SaveOperationsPickupSettingsUseCase>();
        services.AddScoped<Vixly.Operations.Application.UseCases.Pickup.ChangeMyOperationsCollector.ChangeMyOperationsCollectorUseCase>();
        // Slice 7 step 3: home delivery.
        services.AddScoped<Vixly.Operations.Application.Delivery.OperationsDeliveryDesk>();
        services.AddScoped<Vixly.Operations.Application.UseCases.Delivery.GetOperationsDeliveries.GetOperationsDeliveriesUseCase>();
        services.AddScoped<Vixly.Operations.Application.UseCases.Delivery.GetOperationsRunSheet.GetOperationsRunSheetUseCase>();
        services.AddScoped<Vixly.Operations.Application.UseCases.Delivery.BillOperationsDelivery.BillOperationsDeliveryUseCase>();
        services.AddScoped<Vixly.Operations.Application.UseCases.Delivery.PlanOperationsDeliveryRoute.PlanOperationsDeliveryRouteUseCase>();
        services.AddScoped<Vixly.Operations.Application.UseCases.Delivery.GetOperationsDeliveryRoute.GetOperationsDeliveryRouteUseCase>();
        services.AddScoped<Vixly.Operations.Application.UseCases.Delivery.ChangeOperationsDeliveryRoute.ChangeOperationsDeliveryRouteUseCase>();
        services.AddScoped<Vixly.Operations.Application.UseCases.Delivery.StartOperationsDeliveryRoute.StartOperationsDeliveryRouteUseCase>();
        services.AddScoped<Vixly.Operations.Application.UseCases.Delivery.RecordOperationsDeliveryStop.RecordOperationsDeliveryStopUseCase>();
        services.AddScoped<Vixly.Operations.Application.UseCases.Delivery.SettleOperationsDeliveryRoute.SettleOperationsDeliveryRouteUseCase>();
        services.AddScoped<Vixly.Operations.Application.UseCases.Delivery.CancelOperationsDeliveryRoute.CancelOperationsDeliveryRouteUseCase>();
        services.AddScoped<Vixly.Operations.Application.UseCases.Delivery.GetMyOperationsDeliveryRoutes.GetMyOperationsDeliveryRoutesUseCase>();
        services.AddScoped<Vixly.Operations.Application.UseCases.Delivery.ReturnOperationsDeliveryParcel.ReturnOperationsDeliveryParcelUseCase>();
        services.AddScoped<Vixly.Operations.Application.UseCases.Delivery.GetOperationsDeliverySettings.GetOperationsDeliverySettingsUseCase>();
        services.AddScoped<Vixly.Operations.Application.UseCases.Delivery.SaveOperationsDeliverySettings.SaveOperationsDeliverySettingsUseCase>();
        // Slice 7 step 1: destination receiving.
        services.AddScoped<Vixly.Operations.Application.Arrivals.OperationsArrivalDesk>();
        services.AddScoped<Vixly.Operations.Application.UseCases.Arrivals.GetOperationsArrivals.GetOperationsArrivalsUseCase>();
        services.AddScoped<Vixly.Operations.Application.UseCases.Arrivals.GetOperationsArrival.GetOperationsArrivalUseCase>();
        services.AddScoped<Vixly.Operations.Application.UseCases.Arrivals.ScanOperationsArrival.ScanOperationsArrivalUseCase>();
        services.AddScoped<Vixly.Operations.Application.UseCases.Arrivals.OpenOperationsArrivalBundle.OpenOperationsArrivalBundleUseCase>();
        services.AddScoped<Vixly.Operations.Application.UseCases.Arrivals.FinishOperationsArrival.FinishOperationsArrivalUseCase>();
        services.AddScoped<Vixly.Operations.Application.UseCases.Arrivals.RecordOperationsCustoms.RecordOperationsCustomsUseCase>();
        services.AddScoped<Vixly.Operations.Application.UseCases.Arrivals.GetOperationsDiscrepancies.GetOperationsDiscrepanciesUseCase>();
        services.AddScoped<Vixly.Operations.Application.UseCases.Arrivals.ResolveOperationsDiscrepancy.ResolveOperationsDiscrepancyUseCase>();
        // Slice 6C: operational events.
        services.AddScoped<Vixly.Operations.Application.Events.OperationsEventDesk>();
        services.AddScoped<Vixly.Operations.Application.UseCases.Events.FindOperationsEventSubject.FindOperationsEventSubjectUseCase>();
        services.AddScoped<Vixly.Operations.Application.UseCases.Events.GetOperationsEventTimeline.GetOperationsEventTimelineUseCase>();
        services.AddScoped<Vixly.Operations.Application.UseCases.Events.AddOperationsEvent.AddOperationsEventUseCase>();
        services.AddScoped<Vixly.Operations.Application.UseCases.Events.VoidOperationsEvent.VoidOperationsEventUseCase>();
        services.AddScoped<Vixly.Operations.Application.UseCases.Events.GetOperationsEventSettings.GetOperationsEventSettingsUseCase>();
        services.AddScoped<Vixly.Operations.Application.UseCases.Events.SaveOperationsEventSettings.SaveOperationsEventSettingsUseCase>();
        services.AddScoped<Vixly.Operations.Application.UseCases.Events.SaveOperationsEventReason.SaveOperationsEventReasonUseCase>();
        // Slice 6B: carriers, dispatches and manifests.
        services.AddScoped<Vixly.Operations.Application.Dispatch.OperationsDispatching>();
        services.AddScoped<Vixly.Operations.Application.Dispatch.OperationsManifests>();
        services.AddScoped<Vixly.Operations.Application.Dispatch.OperationsDispatchPapers>();
        services.AddScoped<Vixly.Operations.Application.Dispatch.OperationsDispatchCosting>();
        services.AddScoped<Vixly.Operations.Application.UseCases.Dispatch.RecostOperationsDispatch.RecostOperationsDispatchUseCase>();
        services.AddScoped<Vixly.Operations.Application.UseCases.Dispatch.CorrectOperationsDispatchCost.CorrectOperationsDispatchCostUseCase>();
        services.AddScoped<Vixly.Operations.Application.UseCases.Dispatch.SaveOperationsDispatchInstructions.SaveOperationsDispatchInstructionsUseCase>();
        services.AddScoped<Vixly.Operations.Application.UseCases.Dispatch.GetOperationsHouseWaybills.GetOperationsHouseWaybillsUseCase>();
        services.AddScoped<Vixly.Operations.Application.UseCases.Dispatch.GetOperationsShipperLetter.GetOperationsShipperLetterUseCase>();
        services.AddScoped<Vixly.Operations.Application.UseCases.Dispatch.GetOperationsCarriers.GetOperationsCarriersUseCase>();
        services.AddScoped<Vixly.Operations.Application.UseCases.Dispatch.GetOperationsCarrier.GetOperationsCarrierUseCase>();
        services.AddScoped<Vixly.Operations.Application.UseCases.Dispatch.SaveOperationsCarrier.SaveOperationsCarrierUseCase>();
        services.AddScoped<Vixly.Operations.Application.UseCases.Dispatch.AddOperationsCarrierRate.AddOperationsCarrierRateUseCase>();
        services.AddScoped<Vixly.Operations.Application.UseCases.Dispatch.RemoveOperationsCarrierRate.RemoveOperationsCarrierRateUseCase>();
        services.AddScoped<Vixly.Operations.Application.UseCases.Dispatch.GetOperationsDispatches.GetOperationsDispatchesUseCase>();
        services.AddScoped<Vixly.Operations.Application.UseCases.Dispatch.CreateOperationsDispatch.CreateOperationsDispatchUseCase>();
        services.AddScoped<Vixly.Operations.Application.UseCases.Dispatch.GetOperationsDispatch.GetOperationsDispatchUseCase>();
        services.AddScoped<Vixly.Operations.Application.UseCases.Dispatch.AddBundleToOperationsDispatch.AddBundleToOperationsDispatchUseCase>();
        services.AddScoped<Vixly.Operations.Application.UseCases.Dispatch.RemoveBundleFromOperationsDispatch.RemoveBundleFromOperationsDispatchUseCase>();
        services.AddScoped<Vixly.Operations.Application.UseCases.Dispatch.AddOperationsDispatchLeg.AddOperationsDispatchLegUseCase>();
        services.AddScoped<Vixly.Operations.Application.UseCases.Dispatch.RemoveOperationsDispatchLeg.RemoveOperationsDispatchLegUseCase>();
        services.AddScoped<Vixly.Operations.Application.UseCases.Dispatch.CloseOperationsDispatch.CloseOperationsDispatchUseCase>();
        services.AddScoped<Vixly.Operations.Application.UseCases.Dispatch.DepartOperationsDispatchLeg.DepartOperationsDispatchLegUseCase>();
        services.AddScoped<Vixly.Operations.Application.UseCases.Dispatch.ArriveOperationsDispatchLeg.ArriveOperationsDispatchLegUseCase>();
        services.AddScoped<Vixly.Operations.Application.UseCases.Dispatch.CancelOperationsDispatch.CancelOperationsDispatchUseCase>();
        services.AddScoped<Vixly.Operations.Application.UseCases.Dispatch.GetOperationsDispatchManifest.GetOperationsDispatchManifestUseCase>();
        services.AddScoped<Vixly.Operations.Application.UseCases.Dispatch.GetOperationsBundleManifest.GetOperationsBundleManifestUseCase>();
        // Slice 5F step 2: the cashier.
        services.AddScoped<Vixly.Operations.Application.UseCases.Cashier.GetOperationsCashier.GetOperationsCashierUseCase>();
        services.AddScoped<Vixly.Operations.Application.UseCases.Cashier.GetOperationsCashierInvoice.GetOperationsCashierInvoiceUseCase>();
        services.AddScoped<Vixly.Operations.Application.UseCases.Cashier.TakeOperationsPayment.TakeOperationsPaymentUseCase>();
        services.AddScoped<Vixly.Operations.Application.UseCases.Cashier.GetOperationsPayment.GetOperationsPaymentUseCase>();
        services.AddScoped<Vixly.Operations.Application.UseCases.Cashier.GetOperationsPaymentReceipt.GetOperationsPaymentReceiptUseCase>();
        services.AddScoped<Vixly.Operations.Application.UseCases.Cashier.OpenOperationsCashDrawer.OpenOperationsCashDrawerUseCase>();
        services.AddScoped<Vixly.Operations.Application.UseCases.Cashier.CloseOperationsCashDrawer.CloseOperationsCashDrawerUseCase>();
        services.AddScoped<Vixly.Operations.Application.UseCases.Cashier.ListOperationsCashDrawers.ListOperationsCashDrawersUseCase>();
        services.AddScoped<Vixly.Operations.Application.UseCases.Cashier.GetOperationsCashDrawer.GetOperationsCashDrawerUseCase>();
        services.AddScoped<Vixly.Operations.Application.UseCases.Cashier.ListOperationsPaymentsToVerify.ListOperationsPaymentsToVerifyUseCase>();
        services.AddScoped<Vixly.Operations.Application.UseCases.Cashier.CheckOperationsPayment.CheckOperationsPaymentUseCase>();
        services.AddScoped<Vixly.Operations.Application.UseCases.Cashier.RefundOperationsPayment.RefundOperationsPaymentUseCase>();

        // Slice 5F step 4: cards through the company's own Stripe account.
        services.AddScoped<IOperationsCardRepository, OperationsCardRepository>();
        services.AddScoped<IOperationsScreenRepository, OperationsScreenRepository>();
        services.AddScoped<IOperationsPayablesRepository, OperationsPayablesRepository>();
        services.AddScoped<IOperationsTextRepository, OperationsTextRepository>();
        services.AddScoped<IOperationsTextGateway, Vixly.Operations.Infrastructure.Texts.OperationsTwilioGateway>();
        services.AddScoped<IOperationsTextWriter, Vixly.Operations.Infrastructure.Texts.OperationsTextWriter>();
        services.AddScoped<Vixly.Operations.Application.Texts.OperationsTextDesk>();
        services.AddSingleton<IOperationsTextLog, Vixly.Operations.Infrastructure.Texts.OperationsTextLog>();
        services.AddScoped<Vixly.Operations.Application.UseCases.Texts.HandleOperationsTextStatus.HandleOperationsTextStatusUseCase>();
        services.AddScoped<Vixly.Operations.Application.UseCases.Texts.GetOperationsTextSettings.GetOperationsTextSettingsUseCase>();
        services.AddScoped<Vixly.Operations.Application.UseCases.Texts.ConnectOperationsTextProvider.ConnectOperationsTextProviderUseCase>();
        services.AddScoped<Vixly.Operations.Application.UseCases.Texts.DisconnectOperationsTextProvider.DisconnectOperationsTextProviderUseCase>();
        services.AddScoped<Vixly.Operations.Application.UseCases.Texts.SaveOperationsTextNotices.SaveOperationsTextNoticesUseCase>();
        services.AddScoped<Vixly.Operations.Application.UseCases.Customers.GetMyOperationsNotices.GetMyOperationsNoticesUseCase>();
        services.AddScoped<Vixly.Operations.Application.UseCases.Customers.ChangeMyOperationsNotices.ChangeMyOperationsNoticesUseCase>();
        services.AddHttpClient(Vixly.Operations.Infrastructure.Texts.OperationsTwilioGateway.HttpClientName, client =>
        {
            client.BaseAddress = new Uri("https://api.twilio.com/");
            client.Timeout = TimeSpan.FromSeconds(20);
        });
        services.AddScoped<IOperationsReportReader, Vixly.Operations.Infrastructure.Reports.OperationsReportReader>();
        services.AddSingleton<IOperationsReportFiles, Vixly.Operations.Infrastructure.Reports.OperationsReportFiles>();
        services.AddScoped<IOperationsDashboardReader, Vixly.Operations.Infrastructure.Reports.OperationsDashboardReader>();
        services.AddScoped<IOperationsReportScheduleRepository, OperationsReportScheduleRepository>();
        services.AddScoped<IOperationsReportMailer, Vixly.Operations.Infrastructure.Mail.SmtpOperationsReportMailer>();
        services.AddScoped<Vixly.Operations.Application.UseCases.Reports.ListOperationsReportSchedules.ListOperationsReportSchedulesUseCase>();
        services.AddScoped<Vixly.Operations.Application.UseCases.Reports.SaveOperationsReportSchedule.SaveOperationsReportScheduleUseCase>();
        services.AddScoped<Vixly.Operations.Application.UseCases.Reports.RemoveOperationsReportSchedule.RemoveOperationsReportScheduleUseCase>();
        services.AddScoped<Vixly.Operations.Application.UseCases.Dashboard.GetOperationsDashboard.GetOperationsDashboardUseCase>();
        services.AddScoped<Vixly.Operations.Application.UseCases.Reports.ListOperationsReports.ListOperationsReportsUseCase>();
        services.AddScoped<Vixly.Operations.Application.UseCases.Reports.RunOperationsReport.RunOperationsReportUseCase>();
        services.AddScoped<Vixly.Operations.Application.UseCases.Reports.GetOperationsReportRules.GetOperationsReportRulesUseCase>();
        services.AddScoped<Vixly.Operations.Application.UseCases.Reports.SaveOperationsReportRules.SaveOperationsReportRulesUseCase>();
        services.AddScoped<IOperationsApiRepository, OperationsApiRepository>();
        services.AddScoped<IOperationsApiReader, Vixly.Operations.Infrastructure.Api.OperationsApiReader>();
        services.AddScoped<IOperationsWebhookSender, Vixly.Operations.Infrastructure.Api.OperationsWebhookSender>();
        services.AddScoped<Vixly.Operations.Application.Api.OperationsWebhookDesk>();
        services.AddScoped<Vixly.Operations.Application.UseCases.Api.GetOperationsApiSettings.GetOperationsApiSettingsUseCase>();
        services.AddScoped<Vixly.Operations.Application.UseCases.Api.CreateOperationsApiKey.CreateOperationsApiKeyUseCase>();
        services.AddScoped<Vixly.Operations.Application.UseCases.Api.RevokeOperationsApiKey.RevokeOperationsApiKeyUseCase>();
        services.AddScoped<Vixly.Operations.Application.UseCases.Api.SaveOperationsWebhookEndpoint.SaveOperationsWebhookEndpointUseCase>();
        services.AddScoped<Vixly.Operations.Application.UseCases.Api.SwitchOperationsWebhookEndpoint.SwitchOperationsWebhookEndpointUseCase>();
        services.AddScoped<Vixly.Operations.Application.UseCases.Api.SendOperationsWebhookTest.SendOperationsWebhookTestUseCase>();
        services.AddScoped<Vixly.Operations.Application.UseCases.Api.ResendOperationsWebhookDelivery.ResendOperationsWebhookDeliveryUseCase>();
        services.AddScoped<Vixly.Operations.Application.UseCases.Api.AuthenticateOperationsApiKey.AuthenticateOperationsApiKeyUseCase>();
        services.AddScoped<Vixly.Operations.Application.UseCases.Api.QueryOperationsApi.QueryOperationsApiUseCase>();
        services.AddHttpClient(Vixly.Operations.Infrastructure.Api.OperationsWebhookSender.HttpClientName, client =>
        {
            client.Timeout = TimeSpan.FromSeconds(10);
            client.DefaultRequestHeaders.UserAgent.ParseAdd("Webhooks/1.0");
        }).ConfigurePrimaryHttpMessageHandler(Vixly.Operations.Infrastructure.Api.OperationsWebhookSender.Handler);
        services.AddScoped<Vixly.Operations.Application.Payables.OperationsPayablesDesk>();
        services.AddScoped<Vixly.Operations.Application.Payables.OperationsProfitCalculator>();
        services.AddScoped<Vixly.Operations.Application.UseCases.Payables.ListOperationsBills.ListOperationsBillsUseCase>();
        services.AddScoped<Vixly.Operations.Application.UseCases.Payables.GetOperationsBill.GetOperationsBillUseCase>();
        services.AddScoped<Vixly.Operations.Application.UseCases.Payables.SaveOperationsBill.SaveOperationsBillUseCase>();
        services.AddScoped<Vixly.Operations.Application.UseCases.Payables.DecideOperationsBill.DecideOperationsBillUseCase>();
        services.AddScoped<Vixly.Operations.Application.UseCases.Payables.PayOperationsBill.PayOperationsBillUseCase>();
        services.AddScoped<Vixly.Operations.Application.UseCases.Payables.VoidOperationsBill.VoidOperationsBillUseCase>();
        services.AddScoped<Vixly.Operations.Application.UseCases.Payables.ListOperationsVendors.ListOperationsVendorsUseCase>();
        services.AddScoped<Vixly.Operations.Application.UseCases.Payables.SaveOperationsVendor.SaveOperationsVendorUseCase>();
        services.AddScoped<Vixly.Operations.Application.UseCases.Payables.GetOperationsProfit.GetOperationsProfitUseCase>();
        services.AddScoped<Vixly.Operations.Application.UseCases.Payables.GetOperationsShipmentProfit.GetOperationsShipmentProfitUseCase>();
        services.AddScoped<Vixly.Operations.Application.UseCases.Payables.ChangeOperationsBillSettings.ChangeOperationsBillSettingsUseCase>();
        services.AddHttpClient(Vixly.Operations.Infrastructure.Payments.OperationsStripeGateway.HttpClientName, client =>
        {
            client.BaseAddress = new Uri("https://api.stripe.com/");
            client.Timeout = TimeSpan.FromSeconds(30);
        });
        services.AddSingleton<IOperationsCardGateway, Vixly.Operations.Infrastructure.Payments.OperationsStripeGateway>();
        services.AddScoped<Vixly.Operations.Application.Cards.OperationsCardDesk>();
        services.AddScoped<Vixly.Operations.Application.Shipments.OperationsDeclarationWriter>();
        services.AddScoped<Vixly.Operations.Application.Screens.OperationsScreenDesk>();
        services.AddScoped<Vixly.Operations.Application.UseCases.Screens.ListOperationsCustomerScreens.ListOperationsCustomerScreensUseCase>();
        services.AddScoped<Vixly.Operations.Application.UseCases.Screens.PairOperationsCustomerScreen.PairOperationsCustomerScreenUseCase>();
        services.AddScoped<Vixly.Operations.Application.UseCases.Screens.ConnectOperationsCustomerScreen.ConnectOperationsCustomerScreenUseCase>();
        services.AddScoped<Vixly.Operations.Application.UseCases.Screens.RemoveOperationsCustomerScreen.RemoveOperationsCustomerScreenUseCase>();
        services.AddScoped<Vixly.Operations.Application.UseCases.Screens.SendToOperationsCustomerScreen.SendToOperationsCustomerScreenUseCase>();
        services.AddScoped<Vixly.Operations.Application.UseCases.Screens.GetOperationsScreen.GetOperationsScreenUseCase>();
        services.AddScoped<Vixly.Operations.Application.UseCases.Screens.ActOnOperationsScreen.ActOnOperationsScreenUseCase>();
        services.AddScoped<Vixly.Operations.Application.UseCases.Screens.GetOperationsScreenZelleQr.GetOperationsScreenZelleQrUseCase>();
        services.AddScoped<Vixly.Operations.Application.UseCases.Cashier.GetOperationsCardSettings.GetOperationsCardSettingsUseCase>();
        services.AddScoped<Vixly.Operations.Application.UseCases.Cashier.ConnectOperationsCardProvider.ConnectOperationsCardProviderUseCase>();
        services.AddScoped<Vixly.Operations.Application.UseCases.Cashier.DisconnectOperationsCardProvider.DisconnectOperationsCardProviderUseCase>();
        services.AddScoped<Vixly.Operations.Application.UseCases.Cashier.ChangeOperationsCardAcceptance.ChangeOperationsCardAcceptanceUseCase>();
        services.AddScoped<Vixly.Operations.Application.UseCases.Cashier.SaveOperationsCardReader.SaveOperationsCardReaderUseCase>();
        services.AddScoped<Vixly.Operations.Application.UseCases.Cashier.StartOperationsCardPayment.StartOperationsCardPaymentUseCase>();
        services.AddScoped<Vixly.Operations.Application.UseCases.Cashier.GetOperationsCardPayment.GetOperationsCardPaymentUseCase>();
        services.AddScoped<Vixly.Operations.Application.UseCases.Cashier.CancelOperationsCardPayment.CancelOperationsCardPaymentUseCase>();
        services.AddScoped<Vixly.Operations.Application.UseCases.Cashier.HandleOperationsCardWebhook.HandleOperationsCardWebhookUseCase>();
        services.AddScoped<Vixly.Operations.Application.UseCases.Billing.PayMyOperationsInvoiceByCard.PayMyOperationsInvoiceByCardUseCase>();
        services.AddScoped<Vixly.Operations.Application.UseCases.Billing.PayMyOperationsInvoice.PayMyOperationsInvoiceUseCase>();
        services.AddScoped<Vixly.Operations.Application.UseCases.Billing.GetMyOperationsZelleQr.GetMyOperationsZelleQrUseCase>();
        services.AddScoped<Vixly.Operations.Application.UseCases.Billing.GetMyOperationsCardPayment.GetMyOperationsCardPaymentUseCase>();
        services.AddScoped<Vixly.Operations.Application.UseCases.Cashier.GetOperationsPaymentSettings.GetOperationsPaymentSettingsUseCase>();
        services.AddScoped<Vixly.Operations.Application.UseCases.Cashier.SaveOperationsPaymentSettings.SaveOperationsPaymentSettingsUseCase>();
        services.AddScoped<Vixly.Operations.Application.UseCases.Cashier.GetOperationsZelleQr.GetOperationsZelleQrUseCase>();
        services.AddScoped<Vixly.Operations.Application.UseCases.Billing.ListOperationsInvoices.ListOperationsInvoicesUseCase>();
        services.AddScoped<Vixly.Operations.Application.UseCases.Billing.GetOperationsInvoice.GetOperationsInvoiceUseCase>();
        services.AddScoped<Vixly.Operations.Application.UseCases.Billing.GetOperationsInvoiceDocument.GetOperationsInvoiceDocumentUseCase>();
        services.AddScoped<Vixly.Operations.Application.UseCases.Billing.RequestOperationsCredit.RequestOperationsCreditUseCase>();
        services.AddScoped<Vixly.Operations.Application.UseCases.Billing.DecideOperationsCredit.DecideOperationsCreditUseCase>();
        services.AddScoped<Vixly.Operations.Application.UseCases.Billing.ListOperationsCreditRequests.ListOperationsCreditRequestsUseCase>();
        services.AddScoped<Vixly.Operations.Application.UseCases.Billing.GetOperationsInvoiceSettings.GetOperationsInvoiceSettingsUseCase>();
        services.AddScoped<Vixly.Operations.Application.UseCases.Billing.SaveOperationsInvoiceSettings.SaveOperationsInvoiceSettingsUseCase>();
        services.AddScoped<Vixly.Operations.Application.UseCases.Billing.ListMyOperationsInvoices.ListMyOperationsInvoicesUseCase>();
        services.AddScoped<Vixly.Operations.Application.UseCases.Billing.GetMyOperationsInvoiceDocument.GetMyOperationsInvoiceDocumentUseCase>();
        services.AddScoped<Vixly.Operations.Application.UseCases.Counter.CancelOperationsSupplyOrder.CancelOperationsSupplyOrderUseCase>();
        services.AddScoped<Vixly.Operations.Application.UseCases.Supplies.ListOperationsLowSupplies.ListOperationsLowSuppliesUseCase>();
        services.AddScoped<Vixly.Operations.Application.UseCases.Supplies.GetOperationsSupplyStock.GetOperationsSupplyStockUseCase>();
        services.AddScoped<Vixly.Operations.Application.UseCases.Supplies.ReceiveOperationsSupplies.ReceiveOperationsSuppliesUseCase>();
        services.AddScoped<Vixly.Operations.Application.UseCases.Supplies.AdjustOperationsSupplyStock.AdjustOperationsSupplyStockUseCase>();
        services.AddScoped<Vixly.Operations.Application.UseCases.Supplies.SetOperationsSupplyReorderLevel.SetOperationsSupplyReorderLevelUseCase>();
        services.AddScoped<Vixly.Operations.Application.UseCases.Supplies.SendOperationsSupplyTransfer.SendOperationsSupplyTransferUseCase>();
        services.AddScoped<Vixly.Operations.Application.UseCases.Supplies.ReceiveOperationsSupplyTransfer.ReceiveOperationsSupplyTransferUseCase>();
        services.AddScoped<Vixly.Operations.Application.UseCases.Supplies.OpenOperationsSupplyCount.OpenOperationsSupplyCountUseCase>();
        services.AddScoped<Vixly.Operations.Application.UseCases.Supplies.GetOperationsSupplyCount.GetOperationsSupplyCountUseCase>();
        services.AddScoped<Vixly.Operations.Application.UseCases.Supplies.SaveOperationsSupplyCount.SaveOperationsSupplyCountUseCase>();
        services.AddScoped<Vixly.Operations.Application.UseCases.Supplies.ListOperationsSupplyMovements.ListOperationsSupplyMovementsUseCase>();
        services.AddScoped<Vixly.Operations.Application.UseCases.Supplies.SaveOperationsSupplySettings.SaveOperationsSupplySettingsUseCase>();
        services.AddScoped<Vixly.Operations.Application.UseCases.Charges.SaveOperationsChargeExtra.SaveOperationsChargeExtraUseCase>();
        services.AddScoped<Vixly.Operations.Application.UseCases.Charges.SaveOperationsInsurancePlan.SaveOperationsInsurancePlanUseCase>();
        services.AddScoped<Vixly.Operations.Application.UseCases.Charges.PriceOperationsShipment.PriceOperationsShipmentUseCase>();
        services.AddScoped<Vixly.Operations.Application.UseCases.Charges.SaveOperationsQuote.SaveOperationsQuoteUseCase>();
        services.AddScoped<Vixly.Operations.Application.UseCases.Charges.ListOperationsQuotes.ListOperationsQuotesUseCase>();
        services.AddScoped<Vixly.Operations.Application.UseCases.Charges.WithdrawOperationsQuote.WithdrawOperationsQuoteUseCase>();
        services.AddScoped<Vixly.Operations.Application.UseCases.Charges.GetMyOperationsRates.GetMyOperationsRatesUseCase>();
        services.AddScoped<Vixly.Operations.Application.UseCases.Holds.ChangeOperationsUnclaimedDays.ChangeOperationsUnclaimedDaysUseCase>();
        services.AddScoped<Vixly.Operations.Application.Inspection.OperationsInspectionSelection>();
        services.AddScoped<Vixly.Operations.Application.Inspection.OperationsInspectionReader>();
        services.AddScoped<Vixly.Operations.Application.Inspection.OperationsInspectionSteps>();
        services.AddScoped<Vixly.Operations.Application.Inspection.OperationsInspectionPhotos>();
        services.AddScoped<Vixly.Operations.Application.UseCases.Inspection.GetOperationsInspectionSetup.GetOperationsInspectionSetupUseCase>();
        services.AddScoped<Vixly.Operations.Application.UseCases.Inspection.SaveOperationsRestrictedItem.SaveOperationsRestrictedItemUseCase>();
        services.AddScoped<Vixly.Operations.Application.UseCases.Inspection.SaveOperationsInspectionRule.SaveOperationsInspectionRuleUseCase>();
        services.AddScoped<Vixly.Operations.Application.UseCases.Inspection.ChangeOperationsInspectionShare.ChangeOperationsInspectionShareUseCase>();
        services.AddScoped<Vixly.Operations.Application.UseCases.Inspection.CopyOperationsStartingList.CopyOperationsStartingListUseCase>();
        services.AddScoped<Vixly.Operations.Application.UseCases.Inspection.ListOperationsInspectionQueue.ListOperationsInspectionQueueUseCase>();
        services.AddScoped<Vixly.Operations.Application.UseCases.Inspection.GetOperationsInspection.GetOperationsInspectionUseCase>();
        services.AddScoped<Vixly.Operations.Application.UseCases.Inspection.StartOperationsInspection.StartOperationsInspectionUseCase>();
        services.AddScoped<Vixly.Operations.Application.UseCases.Inspection.FinishOperationsInspection.FinishOperationsInspectionUseCase>();
        services.AddScoped<Vixly.Operations.Application.UseCases.Inspection.DecideOperationsDisposal.DecideOperationsDisposalUseCase>();
        services.AddScoped<Vixly.Operations.Application.UseCases.Inspection.ListOperationsHolds.ListOperationsHoldsUseCase>();
        services.AddScoped<Vixly.Operations.Application.UseCases.Inspection.ReleaseOperationsHold.ReleaseOperationsHoldUseCase>();
        services.AddScoped<Vixly.Operations.Application.Receiving.OperationsReceivingScope>();
        services.AddScoped<Vixly.Operations.Application.Receiving.OperationsParcelReader>();
        services.AddScoped<Vixly.Operations.Application.Receiving.OperationsParcelMatcher>();
        services.AddSingleton<Vixly.Operations.Application.Receiving.OperationsInspectionDraw>();
        services.AddScoped<IOperationsPrintingRepository, OperationsPrintingRepository>();
        services.AddScoped<Vixly.Operations.Application.Printing.OperationsLabelPrinting>();
        services.AddScoped<Vixly.Operations.Application.Printing.OperationsPrintAgentKeys>();
        services.AddScoped<Vixly.Operations.Application.UseCases.Receiving.GetOperationsScanStation.GetOperationsScanStationUseCase>();
        services.AddScoped<Vixly.Operations.Application.UseCases.Receiving.OpenOperationsDelivery.OpenOperationsDeliveryUseCase>();
        services.AddScoped<Vixly.Operations.Application.UseCases.Receiving.ScanOperationsParcel.ScanOperationsParcelUseCase>();
        services.AddScoped<Vixly.Operations.Application.UseCases.Receiving.CloseOperationsDelivery.CloseOperationsDeliveryUseCase>();
        services.AddScoped<Vixly.Operations.Application.UseCases.Receiving.GetOperationsReceiptDocument.GetOperationsReceiptDocumentUseCase>();
        services.AddScoped<Vixly.Operations.Application.UseCases.Receiving.ListOperationsReceipts.ListOperationsReceiptsUseCase>();
        services.AddScoped<Vixly.Operations.Application.UseCases.Receiving.FindOperationsParcelByScan.FindOperationsParcelByScanUseCase>();
        services.AddScoped<Vixly.Operations.Application.UseCases.Receiving.CheckOperationsParcel.CheckOperationsParcelUseCase>();
        services.AddScoped<Vixly.Operations.Application.UseCases.Printing.ListOperationsPrinters.ListOperationsPrintersUseCase>();
        services.AddScoped<Vixly.Operations.Application.UseCases.Printing.SaveOperationsPrinter.SaveOperationsPrinterUseCase>();
        services.AddScoped<Vixly.Operations.Application.UseCases.Printing.RegisterOperationsPrintAgent.RegisterOperationsPrintAgentUseCase>();
        services.AddScoped<Vixly.Operations.Application.UseCases.Printing.StopOperationsPrintAgent.StopOperationsPrintAgentUseCase>();
        services.AddScoped<Vixly.Operations.Application.UseCases.Printing.PrintOperationsParcelLabel.PrintOperationsParcelLabelUseCase>();
        services.AddScoped<Vixly.Operations.Application.UseCases.Printing.TakeOperationsPrintJobs.TakeOperationsPrintJobsUseCase>();
        services.AddScoped<Vixly.Operations.Application.UseCases.Printing.FinishOperationsPrintJob.FinishOperationsPrintJobUseCase>();
        services.AddScoped<IOperationsOutboxHandler, Vixly.Operations.Application.Receiving.OperationsParcelNoticeHandler>();
        services.AddScoped<IOperationsOutboxHandler, Vixly.Operations.Application.Tickets.OperationsTicketNoticeHandler>();
        services.AddScoped<IOperationsOutboxHandler, Vixly.Operations.Application.Identity.OperationsDeviceBlockedHandler>();
        services.AddScoped<IOperationsOutboxHandler, Vixly.Operations.Application.Tickets.OperationsTicketEscalatedHandler>();
        services.AddScoped<IOperationsOutboxHandler, Vixly.Operations.Application.Plans.OperationsShipmentCapHandler>();
        services.AddScoped<IOperationsOutboxHandler, Vixly.Operations.Application.Billing.OperationsPaymentNoticeHandler>();
        services.AddScoped<IOperationsOutboxHandler, Vixly.Operations.Application.Billing.OperationsStatementNoticeHandler>();
        services.AddScoped<IOperationsOutboxHandler, Vixly.Operations.Application.Events.OperationsEventNoticeHandler>();
        services.AddScoped<IOperationsOutboxHandler, Vixly.Operations.Application.Arrivals.OperationsPickupNoticeHandler>();
        services.AddScoped<IOperationsOutboxHandler, Vixly.Operations.Application.Pickup.OperationsPickupReminderHandler>();
        services.AddScoped<IOperationsOutboxHandler, Vixly.Operations.Application.Delivery.OperationsOutForDeliveryNoticeHandler>();
        // Operations.Web writes the files and Operations.Api reads them for the portal: both must name the same folder.
        services.AddOptions<Vixly.Operations.Infrastructure.Files.OperationsFileOptions>()
            .Bind(configuration.GetSection(Vixly.Operations.Infrastructure.Files.OperationsFileOptions.SectionName))
            .Validate(options => !string.IsNullOrWhiteSpace(options.Root), "OperationsFiles:Root is not configured.")
            .ValidateOnStart();
        services.AddSingleton<Vixly.Operations.Infrastructure.Files.IOperationsFileProvider, Vixly.Operations.Infrastructure.Files.OperationsFolderFileProvider>();
        services.AddSingleton<IOperationsFileStore, Vixly.Operations.Infrastructure.Files.OperationsFileStore>();
        services.AddOptions<Vixly.Operations.Infrastructure.Documents.OperationsDocumentOptions>().Bind(configuration.GetSection(Vixly.Operations.Infrastructure.Documents.OperationsDocumentOptions.SectionName));
        // Scoped: each document carries the company's own logo (owner, 2026-10-28).
        services.AddScoped<Vixly.Operations.Infrastructure.Documents.OperationsDocumentLogos>();
        services.AddScoped<IOperationsLabelRenderer, Vixly.Operations.Infrastructure.Documents.QuestPdfOperationsLabelRenderer>();
        services.AddSingleton<IOperationsLabelLogoMaker, Vixly.Operations.Infrastructure.Documents.OperationsLabelLogoMaker>();
        services.AddScoped<Vixly.Operations.Application.UseCases.Company.SaveOperationsDocumentLogo.SaveOperationsDocumentLogoUseCase>();
        services.AddScoped<Vixly.Operations.Application.UseCases.Company.RemoveOperationsDocumentLogo.RemoveOperationsDocumentLogoUseCase>();
        services.AddScoped<Vixly.Operations.Application.UseCases.Company.GetOperationsDocumentLogo.GetOperationsDocumentLogoUseCase>();
        services.AddScoped<Vixly.Operations.Application.Charges.OperationsQuoteDocuments>();
        services.AddScoped<Vixly.Operations.Application.UseCases.Charges.GetOperationsQuoteDocument.GetOperationsQuoteDocumentUseCase>();
        services.AddScoped<Vixly.Operations.Application.UseCases.Charges.EmailOperationsQuote.EmailOperationsQuoteUseCase>();
        services.AddScoped<Vixly.Operations.Application.UseCases.Receiving.ListOperationsParcels.ListOperationsParcelsUseCase>();
        services.AddScoped<Vixly.Operations.Application.UseCases.Receiving.ListOperationsOnHandParcels.ListOperationsOnHandParcelsUseCase>();
        services.AddScoped<Vixly.Operations.Application.UseCases.Receiving.GetOperationsParcel.GetOperationsParcelUseCase>();
        services.AddScoped<Vixly.Operations.Application.UseCases.Receiving.GetOperationsParcelPhoto.GetOperationsParcelPhotoUseCase>();
        services.AddScoped<Vixly.Operations.Application.UseCases.Receiving.GetOperationsParcelLabel.GetOperationsParcelLabelUseCase>();
        services.AddScoped<Vixly.Operations.Application.UseCases.Receiving.AssignOperationsParcel.AssignOperationsParcelUseCase>();
        services.AddScoped<Vixly.Operations.Application.UseCases.Receiving.PutAwayOperationsParcel.PutAwayOperationsParcelUseCase>();
        services.AddScoped<Vixly.Operations.Application.UseCases.Receiving.ListOperationsStorageLocations.ListOperationsStorageLocationsUseCase>();
        services.AddScoped<Vixly.Operations.Application.UseCases.Receiving.SaveOperationsStorageLocation.SaveOperationsStorageLocationUseCase>();
        services.AddScoped<Vixly.Operations.Application.UseCases.Receiving.CreateMyOperationsPreAlert.CreateMyOperationsPreAlertUseCase>();

        // Customer-created shipments (owner, 2026-10-18).
        services.AddScoped<Vixly.Operations.Application.Shipments.OperationsShipmentIntake>();
        services.AddScoped<Vixly.Operations.Application.Plans.OperationsPlanGate>();
        services.AddScoped<IOperationsSuspensionLift, Vixly.Operations.Infrastructure.Persistence.OperationsSuspensionLift>();
        services.AddScoped<Vixly.Operations.Application.Shipments.OperationsPreAlertIntake>();

        // Items in parcels and the item catalogue (owner, 2026-11-01).
        services.AddScoped<Vixly.Operations.Application.Items.OperationsItemDeclaration>();
        services.AddScoped<Vixly.Operations.Application.Items.OperationsItemCatalogue>();
        services.AddScoped<Vixly.Operations.Application.Items.OperationsItemCheck>();
        services.AddScoped<Vixly.Operations.Application.UseCases.Items.GetOperationsParcelItems.GetOperationsParcelItemsUseCase>();
        services.AddScoped<Vixly.Operations.Application.UseCases.Items.SaveOperationsParcelItems.SaveOperationsParcelItemsUseCase>();
        services.AddScoped<Vixly.Operations.Application.UseCases.Items.ListOperationsItemsToReview.ListOperationsItemsToReviewUseCase>();
        services.AddScoped<Vixly.Operations.Application.UseCases.Items.GetOperationsItemCatalogue.GetOperationsItemCatalogueUseCase>();
        services.AddScoped<Vixly.Operations.Application.UseCases.Items.SaveOperationsItemCatalogueEntry.SaveOperationsItemCatalogueEntryUseCase>();
        services.AddScoped<Vixly.Operations.Application.UseCases.Items.RemoveOperationsItemCatalogueEntry.RemoveOperationsItemCatalogueEntryUseCase>();
        services.AddScoped<Vixly.Operations.Application.UseCases.Items.ChangeOperationsItemRule.ChangeOperationsItemRuleUseCase>();
        services.AddScoped<Vixly.Operations.Application.Shipments.OperationsShipmentPlacement>();
        services.AddScoped<Vixly.Operations.Application.Shipments.OperationsPackageMover>();
        services.AddScoped<Vixly.Operations.Application.UseCases.Shipments.PlaceMyOperationsPackage.PlaceMyOperationsPackageUseCase>();
        services.AddScoped<Vixly.Operations.Application.UseCases.Receiving.ListOperationsIncoming.ListOperationsIncomingUseCase>();
        services.AddScoped<Vixly.Operations.Application.Printing.OperationsShippingLabels>();
        services.AddScoped<Vixly.Operations.Application.Shipments.OperationsShipmentDocuments>();
        services.AddScoped<Vixly.Operations.Application.Insurance.OperationsLiability>();
        services.AddScoped<Vixly.Operations.Application.Insurance.OperationsCertificates>();
        services.AddScoped<Vixly.Operations.Application.Shipments.OperationsShipmentBooking>();
        services.AddScoped<Vixly.Operations.Application.Charges.OperationsShipmentEstimator>();

        // Slice 5C: the front-desk counter.
        services.AddScoped<Vixly.Operations.Application.Counter.OperationsCounterDesk>();
        services.AddScoped<Vixly.Operations.Application.UseCases.Counter.FindOperationsCounterCustomers.FindOperationsCounterCustomersUseCase>();
        services.AddScoped<Vixly.Operations.Application.UseCases.Counter.GetOperationsCounterCustomer.GetOperationsCounterCustomerUseCase>();
        services.AddScoped<Vixly.Operations.Application.UseCases.Counter.CreateOperationsCounterShipment.CreateOperationsCounterShipmentUseCase>();
        services.AddScoped<Vixly.Operations.Application.UseCases.Counter.ChangeOperationsCounterParcelCount.ChangeOperationsCounterParcelCountUseCase>();
        services.AddScoped<Vixly.Operations.Application.UseCases.Counter.GetOperationsCounterShipment.GetOperationsCounterShipmentUseCase>();
        services.AddScoped<Vixly.Operations.Application.UseCases.Counter.AddOperationsCounterParcel.AddOperationsCounterParcelUseCase>();
        services.AddScoped<Vixly.Operations.Application.UseCases.Counter.ChangeOperationsCounterInsurance.ChangeOperationsCounterInsuranceUseCase>();
        services.AddScoped<Vixly.Operations.Application.UseCases.Counter.GetOperationsCounterDeclaration.GetOperationsCounterDeclarationUseCase>();
        services.AddScoped<Vixly.Operations.Application.UseCases.Counter.SaveOperationsCounterSignature.SaveOperationsCounterSignatureUseCase>();
        services.AddScoped<Vixly.Operations.Application.UseCases.Counter.EstimateOperationsCounterShipment.EstimateOperationsCounterShipmentUseCase>();
        services.AddScoped<Vixly.Operations.Application.UseCases.Shipments.EstimateMyOperationsShipment.EstimateMyOperationsShipmentUseCase>();
        services.AddScoped<Vixly.Operations.Application.Insurance.OperationsCertificateDocuments>();
        services.AddScoped<Vixly.Operations.Application.UseCases.Insurance.ListOperationsInsurance.ListOperationsInsuranceUseCase>();
        services.AddScoped<Vixly.Operations.Application.UseCases.Insurance.GetOperationsInsurancePolicy.GetOperationsInsurancePolicyUseCase>();
        services.AddScoped<Vixly.Operations.Application.UseCases.Insurance.SaveOperationsInsurancePolicy.SaveOperationsInsurancePolicyUseCase>();
        services.AddScoped<Vixly.Operations.Application.UseCases.Insurance.ArchiveOperationsInsurancePolicy.ArchiveOperationsInsurancePolicyUseCase>();
        services.AddScoped<Vixly.Operations.Application.UseCases.Insurance.AttachOperationsPolicyDocument.AttachOperationsPolicyDocumentUseCase>();
        services.AddScoped<Vixly.Operations.Application.UseCases.Insurance.RemoveOperationsPolicyDocument.RemoveOperationsPolicyDocumentUseCase>();
        services.AddScoped<Vixly.Operations.Application.UseCases.Insurance.GetOperationsPolicyDocument.GetOperationsPolicyDocumentUseCase>();
        services.AddScoped<Vixly.Operations.Application.UseCases.Insurance.LinkOperationsCoverPolicy.LinkOperationsCoverPolicyUseCase>();
        services.AddScoped<Vixly.Operations.Application.UseCases.Insurance.SaveOperationsInsuranceSettings.SaveOperationsInsuranceSettingsUseCase>();
        services.AddScoped<Vixly.Operations.Application.UseCases.Insurance.SaveOperationsLiabilityTerms.SaveOperationsLiabilityTermsUseCase>();
        services.AddScoped<Vixly.Operations.Application.UseCases.Insurance.GetOperationsInsuranceCertificate.GetOperationsInsuranceCertificateUseCase>();
        services.AddScoped<Vixly.Operations.Application.UseCases.Insurance.GetMyOperationsInsuranceCertificate.GetMyOperationsInsuranceCertificateUseCase>();
        services.AddScoped<Vixly.Operations.Application.UseCases.Shipments.ListOperationsShipments.ListOperationsShipmentsUseCase>();
        services.AddScoped<Vixly.Operations.Application.UseCases.Shipments.GetOperationsShipment.GetOperationsShipmentUseCase>();
        services.AddScoped<Vixly.Operations.Application.UseCases.Shipments.ChangeOperationsShipmentService.ChangeOperationsShipmentServiceUseCase>();
        services.AddScoped<Vixly.Operations.Application.UseCases.Shipments.GetOperationsShipmentDocument.GetOperationsShipmentDocumentUseCase>();
        services.AddScoped<Vixly.Operations.Application.UseCases.Shipments.GetMyOperationsShipmentDocument.GetMyOperationsShipmentDocumentUseCase>();
        services.AddScoped<Vixly.Operations.Application.UseCases.Holds.ChangeOperationsStorageFee.ChangeOperationsStorageFeeUseCase>();
        services.AddScoped<Vixly.Operations.Application.UseCases.Receiving.RecordOperationsParcelLabel.RecordOperationsParcelLabelUseCase>();
        services.AddScoped<Vixly.Operations.Application.UseCases.Holds.StartOperationsStockCount.StartOperationsStockCountUseCase>();
        services.AddScoped<Vixly.Operations.Application.UseCases.Holds.GetOperationsStockCount.GetOperationsStockCountUseCase>();
        services.AddScoped<Vixly.Operations.Application.UseCases.Holds.MarkOperationsStockCount.MarkOperationsStockCountUseCase>();
        services.AddScoped<Vixly.Operations.Application.UseCases.Holds.CloseOperationsStockCount.CloseOperationsStockCountUseCase>();
        services.AddScoped<Vixly.Operations.Application.UseCases.Holds.ListOperationsStockCounts.ListOperationsStockCountsUseCase>();
        services.AddScoped<Vixly.Operations.Application.UseCases.Tickets.CreateMyOperationsTicket.CreateMyOperationsTicketUseCase>();
        services.AddScoped<Vixly.Operations.Application.UseCases.Tickets.ListMyOperationsTickets.ListMyOperationsTicketsUseCase>();
        services.AddScoped<Vixly.Operations.Application.UseCases.Tickets.GetMyOperationsTicket.GetMyOperationsTicketUseCase>();
        services.AddScoped<Vixly.Operations.Application.UseCases.Tickets.ReplyMyOperationsTicket.ReplyMyOperationsTicketUseCase>();
        services.AddScoped<Vixly.Operations.Application.UseCases.Tickets.ListOperationsTickets.ListOperationsTicketsUseCase>();
        services.AddScoped<Vixly.Operations.Application.UseCases.Tickets.GetOperationsTicket.GetOperationsTicketUseCase>();
        services.AddScoped<Vixly.Operations.Application.UseCases.Tickets.AnswerOperationsTicket.AnswerOperationsTicketUseCase>();
        services.AddScoped<Vixly.Operations.Application.UseCases.Tickets.GetOperationsTicketProof.GetOperationsTicketProofUseCase>();

        // Owner, 2026-10-29: the ticket desk (departments, taking, moving, escalations).
        services.AddScoped<Vixly.Operations.Application.Tickets.OperationsTicketScope>();
        services.AddScoped<Vixly.Operations.Application.Tickets.OperationsTicketPeople>();
        services.AddScoped<Vixly.Operations.Application.Tickets.OperationsTicketEscalations>();

        // Owner, 2026-10-29: the plan's monthly shipment cap.
        services.AddScoped<Vixly.Operations.Application.Plans.OperationsShipmentCap>();
        services.AddScoped<Vixly.Operations.Application.Plans.OperationsShipmentCapReport>();
        services.AddScoped<Vixly.Operations.Application.UseCases.Plans.GetOperationsShipmentCap.GetOperationsShipmentCapUseCase>();
        services.AddScoped<Vixly.Operations.Application.UseCases.Tickets.TakeOperationsTicket.TakeOperationsTicketUseCase>();
        services.AddScoped<Vixly.Operations.Application.UseCases.Tickets.MoveOperationsTicket.MoveOperationsTicketUseCase>();
        services.AddScoped<Vixly.Operations.Application.UseCases.Tickets.ChangeOperationsTicketSettings.ChangeOperationsTicketSettingsUseCase>();
        services.AddScoped<Vixly.Operations.Application.UseCases.Printing.PrintOperationsShippingLabel.PrintOperationsShippingLabelUseCase>();
        services.AddScoped<Vixly.Operations.Application.UseCases.Printing.PrintOperationsShipmentLabels.PrintOperationsShipmentLabelsUseCase>();
        services.AddScoped<Vixly.Operations.Application.UseCases.Printing.ListOperationsLabelPrinters.ListOperationsLabelPrintersUseCase>();
        services.AddScoped<Vixly.Operations.Application.UseCases.Printing.GetOperationsShippingLabel.GetOperationsShippingLabelUseCase>();
        services.AddScoped<Vixly.Operations.Application.UseCases.Printing.ListOperationsLabelsToPrint.ListOperationsLabelsToPrintUseCase>();
        services.AddScoped<Vixly.Operations.Application.UseCases.Shipments.GetMyOperationsShipmentChoices.GetMyOperationsShipmentChoicesUseCase>();
        services.AddScoped<Vixly.Operations.Application.UseCases.Shipments.CreateMyOperationsShipment.CreateMyOperationsShipmentUseCase>();
        services.AddScoped<Vixly.Operations.Application.UseCases.Shipments.GetMyOperationsShipment.GetMyOperationsShipmentUseCase>();
        services.AddScoped<Vixly.Operations.Application.UseCases.Shipments.ChangeMyOperationsShipment.ChangeMyOperationsShipmentUseCase>();
        services.AddScoped<Vixly.Operations.Application.UseCases.Shipments.CancelMyOperationsShipment.CancelMyOperationsShipmentUseCase>();
        services.AddScoped<Vixly.Operations.Application.UseCases.Receiving.CancelMyOperationsPreAlert.CancelMyOperationsPreAlertUseCase>();
        services.AddScoped<Vixly.Operations.Application.UseCases.Receiving.ListMyOperationsPackages.ListMyOperationsPackagesUseCase>();
        services.AddScoped<Vixly.Operations.Application.UseCases.Receiving.GetMyOperationsParcel.GetMyOperationsParcelUseCase>();
        services.AddScoped<Vixly.Operations.Application.UseCases.Receiving.GetMyOperationsParcelPhoto.GetMyOperationsParcelPhotoUseCase>();

        // Settings and the audit log (Slice 1E).
        services.AddScoped<Vixly.Operations.Application.UseCases.Settings.ListOperationsShippingServices.ListOperationsShippingServicesUseCase>();
        services.AddScoped<Vixly.Operations.Application.UseCases.Settings.SaveOperationsShippingServices.SaveOperationsShippingServicesUseCase>();
        services.AddScoped<Vixly.Operations.Application.UseCases.Settings.GetOperationsSetupChecklist.GetOperationsSetupChecklistUseCase>();
        services.AddScoped<Vixly.Operations.Application.UseCases.Account.ChangeMyOperationsLanguage.ChangeMyOperationsLanguageUseCase>();
        services.AddScoped<Vixly.Operations.Application.UseCases.Company.GetOperationsCompany.GetOperationsCompanyUseCase>();
        services.AddScoped<Vixly.Operations.Application.UseCases.Company.UpdateOperationsCompanyContacts.UpdateOperationsCompanyContactsUseCase>();
        services.AddScoped<Vixly.Operations.Application.UseCases.Company.SubmitOperationsCompanyChange.SubmitOperationsCompanyChangeUseCase>();
        services.AddScoped<Vixly.Operations.Application.UseCases.Company.UpdateOperationsCompanyProfile.UpdateOperationsCompanyProfileUseCase>();
        services.AddScoped<Vixly.Operations.Application.UseCases.Company.WithdrawOperationsCompanyChange.WithdrawOperationsCompanyChangeUseCase>();
        services.AddScoped<Vixly.Operations.Application.UseCases.Company.GetOperationsBilling.GetOperationsBillingUseCase>();
        services.AddScoped<Vixly.Operations.Application.UseCases.Company.CreateOperationsPayLink.CreateOperationsPayLinkUseCase>();
        services.AddScoped<Vixly.Operations.Application.UseCases.Company.GetOperationsBillingDocument.GetOperationsBillingDocumentUseCase>();
        services.AddScoped<Vixly.Operations.Application.UseCases.Audit.ListOperationsAuditEntries.ListOperationsAuditEntriesUseCase>();
        services.AddScoped<Vixly.Operations.Application.UseCases.Audit.ListOperationsMemberActivity.ListOperationsMemberActivityUseCase>();
        services.AddScoped<Vixly.Operations.Application.UseCases.Settings.GetOperationsTenantSettings.GetOperationsTenantSettingsUseCase>();
        services.AddScoped<Vixly.Operations.Application.UseCases.Settings.UpdateOperationsTenantSettings.UpdateOperationsTenantSettingsUseCase>();
        services.AddScoped<ChangeMyOperationsPasswordUseCase>();
        services.AddScoped<ConfirmOperationsSecondFactorUseCase>();
        services.AddScoped<EndMyOperationsSessionUseCase>();
        services.AddScoped<GetMyOperationsAccountUseCase>();
        services.AddScoped<RegenerateMyRecoveryCodesUseCase>();
        services.AddScoped(provider => new OperationsOwnerCheck(provider.GetRequiredService<IOperationsUserRepository>(), provider.GetRequiredService<OperationsAccess>()));
        services.AddScoped<GetOperationsPublicSiteUseCase>();
        services.AddScoped<GetOperationsSiteImageUseCase>();
        services.AddScoped<GetOperationsSiteDraftUseCase>();
        services.AddScoped<UpdateOperationsSiteDraftUseCase>();
        services.AddScoped<UploadOperationsSiteImageUseCase>();
        services.AddScoped<RemoveOperationsSiteImageUseCase>();
        services.AddScoped<PublishOperationsSiteUseCase>();
        services.AddScoped<TrackOperationsShipmentPubliclyUseCase>();
        services.AddScoped<ListOperationsLocationsUseCase>();
        services.AddScoped<SaveOperationsBranchUseCase>();
        services.AddScoped<SaveOperationsWarehouseUseCase>();
        services.AddScoped<SaveOperationsCountryContactUseCase>();
        services.AddScoped<RemoveOperationsCountryContactUseCase>();

        services.AddOptions<OperationsPlatformOptions>().Bind(configuration.GetSection(OperationsPlatformOptions.SectionName));
        services.AddSingleton<OperationsServiceTokenSigner>();
        services.AddHttpClient(OperationsPlatformClient.HttpClientName, (provider, client) =>
        {
            OperationsPlatformOptions options = provider.GetRequiredService<IOptions<OperationsPlatformOptions>>().Value;

            if (!string.IsNullOrWhiteSpace(options.BaseUrl))
            {
                client.BaseAddress = new Uri(options.BaseUrl.TrimEnd('/') + "/");
            }

            client.Timeout = TimeSpan.FromSeconds(Math.Clamp(options.TimeoutSeconds, 1, 60));
        });
        services.AddSingleton<IOperationsPlatformClient, OperationsPlatformClient>();
        services.AddSingleton<OperationsMaintenanceReader>();
        services.AddSingleton<IOperationsTenantDirectory, OperationsTenantDirectory>();

        services.AddScoped<ResolveOperationsTenantUseCase>();
        services.AddScoped<Vixly.Operations.Application.UseCases.Settings.AdoptOperationsCompanyTimeZone.AdoptOperationsCompanyTimeZoneUseCase>();
        services.AddScoped<OperationsOutboxWriter>();
        services.AddScoped<OperationsOutboxDispatcher>();
        services.AddScoped<OperationsPlatformFeedClock>();

        services.AddHealthChecks()
            .AddCheck<OperationsSchemaHealthCheck>("operations-schema", HealthStatus.Unhealthy, new[] { "ready" });

        return services;
    }

    private static void TryAddTimeProvider(this IServiceCollection services)
    {
        if (!services.Any(descriptor => descriptor.ServiceType == typeof(TimeProvider)))
        {
            services.AddSingleton(TimeProvider.System);
        }
    }
}
