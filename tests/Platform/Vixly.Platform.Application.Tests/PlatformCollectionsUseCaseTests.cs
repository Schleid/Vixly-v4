using Vixly.Platform.Application.Collections;
using Vixly.Platform.Application.Messaging;
using Vixly.Platform.Application.Payments;
using Vixly.Platform.Application.Security;
using Vixly.Platform.Application.UseCases.Collections.AddPlatformCollectionNote;
using Vixly.Platform.Application.UseCases.Collections.CheckPlatformTenantCollections;
using Vixly.Platform.Application.UseCases.Collections.GetPlatformCollectionCase;
using Vixly.Platform.Application.UseCases.Collections.GrantPlatformCollectionExtension;
using Vixly.Platform.Application.UseCases.Collections.UpdatePlatformCollectionsSettings;
using Vixly.Platform.Application.UseCases.Subscriptions.GetPlatformTenantEntitlements;
using Vixly.Platform.Domain.Billing;
using Vixly.Platform.Domain.Collections;
using Vixly.Platform.Domain.Payments;
using Vixly.Platform.Domain.Subscriptions;
using Vixly.Platform.Domain.Tenants;

namespace Vixly.Platform.Application.Tests;

// =========================================================
// FILE: PlatformCollectionsUseCaseTests.cs
//
// PURPOSE:
// Proves Slice 6 collections over fakes: the clock's timeline, blocking and clearing, retries, extensions, emails and settings.
//
// RESPONSIBILITIES:
// - One case per tenant; one email per step; block after a final warning; payment clears in the same commit
// - Retries only with consent and a retryable failure, once per invoice and day
// - Extensions with permission, step-up and limits; notes stay internal
//
// IMPORTANT NOTES:
// - Tests assert codes and states, never text (L-1)
// =========================================================
public sealed class PlatformCollectionsUseCaseTests
{
    private readonly CollectionsTestKit _kit = new();

    private static PlatformCollectionsNoticeKind[] Kinds(IEnumerable<PlatformCollectionsNotice> notices) => notices.Select(item => item.Kind).ToArray();

    [Fact]
    public async Task Clock_RemindsWarnsAndBlocks_OnTheTimeline_ThenPaymentRestoresAccess()
    {
        (PlatformTenant tenant, PlatformSubscription subscription, PlatformInvoice invoice) = _kit.Overdue();

        _kit.Day(1, invoice);
        await _kit.RunAsync();
        await _kit.RunAsync();
        PlatformCollectionCase collection = Assert.Single(_kit.Store.Cases);
        Assert.Equal(PlatformCollectionStage.Reminder1, collection.Stage);

        _kit.Day(5, invoice);
        await _kit.RunAsync();
        _kit.Day(10, invoice);
        await _kit.RunAsync();
        Assert.Equal(PlatformCollectionStage.FinalWarning, collection.Stage);
        Assert.Equal(PlatformSubscriptionStatus.Active, subscription.Status);

        _kit.Day(14, invoice);
        await _kit.RunAsync();
        Assert.Equal(PlatformSubscriptionStatus.BlockedForNonPayment, subscription.Status);
        Assert.Equal(
            new[] { PlatformCollectionsNoticeKind.Reminder1, PlatformCollectionsNoticeKind.Reminder2, PlatformCollectionsNoticeKind.FinalWarning, PlatformCollectionsNoticeKind.Blocked },
            Kinds(_kit.Notices()));

        var entitlements = await new GetPlatformTenantEntitlementsUseCase(_kit.Kit.Subscriptions(), _kit.Store, _kit.Kit.Tenants.LegalGate()).ExecuteAsync(
            _kit.Kit.Staff(PlatformPermissions.SubscriptionsView), new GetPlatformTenantEntitlementsRequest { PlatformTenantId = tenant.Id }, CancellationToken.None);
        Assert.Equal(PlatformSubscriptionAccess.ReadOnly, entitlements.Entitlements!.Access);
        Assert.Equal(120m, entitlements.Entitlements.NonPayment!.AmountDue.Single().Amount);

        _kit.Day(21, invoice);
        await _kit.RunAsync();
        Assert.Equal(PlatformSubscriptionAccess.PaymentRequired, subscription.AccessAt(_kit.Kit.Now, _kit.Store.Settings!.AccessRules));
        Assert.Contains(_kit.Store.Events, item => item.EventCode == PlatformCollectionEventCodes.NoAccess);

        await _kit.PayAsync(invoice);
        Assert.Equal(PlatformSubscriptionStatus.Active, subscription.Status);

        await _kit.RunAsync();
        Assert.False(collection.IsOpen);
        Assert.Equal(PlatformCollectionCloseReason.Settled, collection.CloseReason);
        Assert.Equal(PlatformCollectionsNoticeKind.AccessRestored, _kit.Notices()[^1].Kind);
    }

    [Fact]
    public async Task CatchUp_SendsOneStep_AndBlocksOnlyAfterNotice()
    {
        (_, PlatformSubscription subscription, PlatformInvoice invoice) = _kit.Overdue();

        _kit.Day(13, invoice);
        await _kit.RunAsync();
        _kit.Day(14, invoice);
        await _kit.RunAsync();

        Assert.Equal(new[] { PlatformCollectionsNoticeKind.FinalWarning }, Kinds(_kit.Notices()));
        Assert.NotEqual(PlatformSubscriptionStatus.BlockedForNonPayment, subscription.Status);

        _kit.Day(16, invoice);
        await _kit.RunAsync();
        Assert.Equal(PlatformSubscriptionStatus.BlockedForNonPayment, subscription.Status);
    }

    [Fact]
    public async Task Emails_ListTheOverdueInvoices_WithPayLinks_FinalWarningAlsoToTheOwner_AndNothingOnceSettled()
    {
        _kit.Payments.Ready();
        (PlatformTenant tenant, _, PlatformInvoice invoice) = _kit.Overdue();
        tenant.ReplaceContacts(new[]
        {
            PlatformTenantContactDetails.Create(PlatformTenantContactRole.Owner, "Grace Hopper", Vixly.Shared.Kernel.ValueObjects.EmailAddress.Create("owner@atlas.test"), null),
            PlatformTenantContactDetails.Create(PlatformTenantContactRole.Billing, "Ada Billing", Vixly.Shared.Kernel.ValueObjects.EmailAddress.Create("billing@atlas.test"), null)
        }, _kit.Kit.Now);

        _kit.Day(10, invoice);
        await _kit.RunAsync();
        await _kit.DeliverAsync();

        Assert.Equal(new[] { "billing@atlas.test", "owner@atlas.test" }, _kit.Store.Mails.Select(item => item.To));
        PlatformCollectionsMail mail = _kit.Store.Mails[0].Mail;
        Assert.Equal(PlatformCollectionsNoticeKind.FinalWarning, mail.Kind);
        Assert.NotNull(mail.BlockDateUtc);
        Assert.NotNull(Assert.Single(mail.Invoices).PayLink);
        Assert.Single(_kit.Payments.Payments.Links);

        _kit.Store.Mails.Clear();
        _kit.Day(11, invoice);
        _kit.Kit.Tenants.Tenants.Outbox.Add(Vixly.Platform.Domain.Messaging.PlatformOutboxMessage.Create(PlatformMessageTypes.CollectionsNotice, tenant.Id,
            System.Text.Json.JsonSerializer.Serialize(new PlatformCollectionsNotice(tenant.Id, null, PlatformCollectionsNoticeKind.Reminder2, null, null, _kit.Kit.Now)), _kit.Kit.Now));
        await _kit.PayAsync(invoice);
        await _kit.DeliverAsync();
        Assert.Empty(_kit.Store.Mails);
    }

    [Fact]
    public async Task Retry_OnlyForConsentAndARetryableFailure_OncePerInvoiceAndDay()
    {
        _kit.Payments.Ready();
        (PlatformTenant tenant, _, PlatformInvoice invoice) = _kit.Overdue();
        _kit.Payments.SaveCard(tenant.Id, autoCharge: true);
        PlatformCardTransaction first = PlatformCardTransaction.Charge(tenant.Id, invoice.Id, PlatformCardTransactionOrigin.Automatic, PlatformCardProvider.Stripe, invoice.Balance, "USD", null, null, _kit.Kit.Now);
        first.Fail(null, "insufficient_funds", _kit.Kit.Now);
        _kit.Payments.Payments.Transactions.Add(first);
        _kit.Payments.Gateway.NextCharge = new PlatformCardResult("pi_retry", PlatformCardOutcome.Failed, "lost_card");

        _kit.Day(2, invoice);
        await _kit.RunAsync();
        Assert.Empty(_kit.Payments.Gateway.Charges);

        _kit.Day(3, invoice);
        await _kit.RunAsync();
        await _kit.RunAsync();
        Assert.Single(_kit.Payments.Gateway.Charges);
        Assert.Equal(PlatformCardTransactionOrigin.Retry, _kit.Payments.Payments.Transactions[^1].Origin);
        Assert.Single(_kit.Store.Events, item => item.EventCode == PlatformCollectionEventCodes.CardRetry);

        // The retry failed for good (a lost card): day 7 tries nothing.
        _kit.Day(7, invoice);
        await _kit.RunAsync();
        Assert.Single(_kit.Payments.Gateway.Charges);
    }

    [Fact]
    public async Task Extension_NeedsPermission_LiftsTheBlock_AndPastTheLimitNeedsTheHigherPermission()
    {
        (_, PlatformSubscription subscription, PlatformInvoice invoice) = _kit.Overdue();
        _kit.Day(10, invoice);
        await _kit.RunAsync();
        _kit.Day(14, invoice);
        await _kit.RunAsync();
        PlatformCollectionCase collection = Assert.Single(_kit.Store.Cases);

        GrantPlatformCollectionExtensionUseCase useCase = new(_kit.Administration());
        GrantPlatformCollectionExtensionRequest request = new() { CaseId = collection.Id, Through = _kit.Kit.Now.UtcDateTime.Date.AddDays(5), Reason = "Cheque in the post" };

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => useCase.ExecuteAsync(_kit.Kit.Staff(PlatformPermissions.CollectionsView), request, CancellationToken.None));

        var clerk = _kit.Kit.Staff(PlatformPermissions.CollectionsView, PlatformPermissions.CollectionsExtend);
        Assert.True((await useCase.ExecuteAsync(clerk, request, CancellationToken.None)).Succeeded);
        Assert.Equal(PlatformSubscriptionStatus.Active, subscription.Status);
        Assert.Equal(PlatformCollectionsNoticeKind.ExtensionGranted, _kit.Notices()[^1].Kind);
        Assert.True((await useCase.ExecuteAsync(clerk, request, CancellationToken.None)).Succeeded);
        Assert.Equal("platform.collections.extension_limit", (await useCase.ExecuteAsync(clerk, request, CancellationToken.None)).Error!.Code);
        Assert.True((await useCase.ExecuteAsync(_kit.Kit.Staff(PlatformPermissions.CollectionsExtendBeyondLimit), request, CancellationToken.None)).Succeeded);

        var detail = await new GetPlatformCollectionCaseUseCase(_kit.Administration()).ExecuteAsync(clerk, new GetPlatformCollectionCaseRequest { CaseId = collection.Id }, CancellationToken.None);
        Assert.Equal(3, detail.Detail!.Case.ExtensionsUsed);
        Assert.Contains(detail.Detail.History, item => item.EventCode == PlatformCollectionEventCodes.ExtensionGranted && item.Note == "Cheque in the post");
    }

    [Fact]
    public async Task Notes_AndCheckAgain_NeedManage_AndStayInTheHistory()
    {
        (PlatformTenant tenant, _, PlatformInvoice invoice) = _kit.Overdue();
        _kit.Day(1, invoice);
        var manager = _kit.Kit.Staff(PlatformPermissions.CollectionsManage);

        Assert.True((await new CheckPlatformTenantCollectionsUseCase(_kit.Administration()).ExecuteAsync(manager, new CheckPlatformTenantCollectionsRequest { PlatformTenantId = tenant.Id }, CancellationToken.None)).Succeeded);
        PlatformCollectionCase collection = Assert.Single(_kit.Store.Cases);

        AddPlatformCollectionNoteUseCase note = new(_kit.Administration());
        Assert.Equal("platform.collections.note_invalid", (await note.ExecuteAsync(manager, new AddPlatformCollectionNoteRequest { CaseId = collection.Id, Note = " " }, CancellationToken.None)).Error!.Code);
        Assert.True((await note.ExecuteAsync(manager, new AddPlatformCollectionNoteRequest { CaseId = collection.Id, Note = "Called the owner" }, CancellationToken.None)).Succeeded);

        Assert.Contains(_kit.Store.Events, item => item.EventCode == PlatformCollectionEventCodes.Checked);
        Assert.Contains(_kit.Store.Events, item => item.EventCode == PlatformCollectionEventCodes.Note);
    }

    [Fact]
    public async Task PendingPayment_IsBlocked_AfterItsReadOnlyDays_OnlyWithAnOverdueInvoiceToPay()
    {
        PlatformTenant tenant = _kit.Kit.Tenants.Tenant("orbit", active: true);
        PlatformSubscription subscription = _kit.Kit.Subscribe(tenant.Id, _kit.Kit.Plan(), paid: false);
        _kit.Store.UnpaidTenants = () => new[] { tenant.Id };

        // No invoice yet (for example the first invoice was never issued): nothing to pay, no block.
        await _kit.RunAsync();
        Assert.Equal(PlatformSubscriptionStatus.PendingPayment, subscription.Status);
        Assert.Empty(_kit.Notices());

        PlatformInvoice invoice = _kit.FirstPeriodInvoice(tenant.Id, subscription.Id);
        _kit.Day(1, invoice);
        await _kit.RunAsync();

        Assert.Equal(PlatformSubscriptionStatus.BlockedForNonPayment, subscription.Status);
        Assert.Contains(PlatformCollectionsNoticeKind.Blocked, Kinds(_kit.Notices()));
    }

    [Fact]
    public async Task SuspendedAccount_PausesCollections_AndAClosedOneEndsTheCase()
    {
        (PlatformTenant tenant, _, PlatformInvoice invoice) = _kit.Overdue();
        _kit.Day(1, invoice);
        await _kit.RunAsync();

        tenant.Suspend(PlatformSuspensionCategory.Compliance, "Review", Guid.NewGuid(), _kit.Kit.Now);
        _kit.Day(5, invoice);
        await _kit.RunAsync();
        Assert.Single(_kit.Notices());

        tenant.Deactivate("Closed", Guid.NewGuid(), _kit.Kit.Now);
        await _kit.RunAsync();
        Assert.Equal(PlatformCollectionCloseReason.TenantClosed, Assert.Single(_kit.Store.Cases).CloseReason);
    }

    [Fact]
    public async Task Settings_NeedConfigureAndStepUp_AndRefuseABadTimeline()
    {
        UpdatePlatformCollectionsSettingsUseCase useCase = new(_kit.Administration());
        UpdatePlatformCollectionsSettingsRequest good = new()
        {
            Enabled = true, Reminder1Day = 1, Reminder2Day = 7, FinalWarningDay = 14, BlockDay = 21, RetryDays = new[] { 3, 7 }, MinimumNoticeDays = 3,
            BlockedReadOnlyDays = 0, ExtensionMaxDays = 14, ExtensionsPerCase = 2, PendingPaymentReadOnlyDays = 7, LongBlockDays = 60
        };

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => useCase.ExecuteAsync(_kit.Kit.Staff(PlatformPermissions.CollectionsView), good, CancellationToken.None));

        var cfo = _kit.Kit.Staff(PlatformPermissions.CollectionsConfigure);
        Assert.Equal("platform.collections.settings_invalid", (await useCase.ExecuteAsync(cfo, new UpdatePlatformCollectionsSettingsRequest { Reminder1Day = 3, Reminder2Day = 2, FinalWarningDay = 4, BlockDay = 5, MinimumNoticeDays = 1, ExtensionMaxDays = 1, LongBlockDays = 60 }, CancellationToken.None)).Error!.Code);
        Assert.True((await useCase.ExecuteAsync(cfo, good, CancellationToken.None)).Succeeded);
        Assert.Equal(21, _kit.Store.Settings!.BlockDay);
        Assert.Equal(0, _kit.Store.Settings.BlockedReadOnlyDays);
    }
}
