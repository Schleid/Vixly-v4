using Vixly.Platform.Application.Messaging;
using Vixly.Platform.Application.Security;
using Vixly.Platform.Application.UseCases.Payments;
using Vixly.Platform.Application.UseCases.Payments.ConfirmPlatformZelleClaim;
using Vixly.Platform.Application.UseCases.Payments.GetPlatformZelleQrCode;
using Vixly.Platform.Application.UseCases.Payments.ListPlatformZelleClaims;
using Vixly.Platform.Application.UseCases.Payments.OpenPlatformPaymentLink;
using Vixly.Platform.Application.UseCases.Payments.RejectPlatformZelleClaim;
using Vixly.Platform.Application.UseCases.Payments.ReportPlatformZellePayment;
using Vixly.Platform.Application.UseCases.Payments.SetPlatformZelleQrCode;
using Vixly.Platform.Application.UseCases.Payments.UpdatePlatformZelleAccount;
using Vixly.Platform.Domain.Billing;
using Vixly.Platform.Domain.Collections;
using Vixly.Platform.Domain.Messaging;
using Vixly.Platform.Domain.Payments;
using Vixly.Platform.Domain.Subscriptions;
using Vixly.Platform.Domain.Tenants;

namespace Vixly.Platform.Application.Tests;

// =========================================================
// FILE: PlatformZelleUseCaseTests.cs
//
// PURPOSE:
// Proves Zelle at checkout (Slice 5 update): settings, the checkout offer, customer reports and staff verification.
//
// RESPONSIBILITIES:
// - Settings need the payments permission and step-up; the QR image is public only while Zelle is on
// - Checkout offers Zelle for open US-dollar invoices only
// - A report never pays; staff confirm (Zelle payment recorded) or reject (customer told why)
// - A waiting report pauses collections
//
// IMPORTANT NOTES:
// - Tests assert codes and states, never text (L-1)
// =========================================================
public sealed class PlatformZelleUseCaseTests
{
    private static readonly byte[] Png = { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 1, 2, 3, 4 };

    private readonly PaymentsTestKit _kit = new();

    private async Task TurnOnAsync()
    {
        var context = _kit.Kit.Staff(PlatformPermissions.PaymentsConfigure);
        Assert.True((await new UpdatePlatformZelleAccountUseCase(_kit.ZelleAdministration()).ExecuteAsync(context,
            new UpdatePlatformZelleAccountRequest { Enabled = true, RecipientName = "Vixly Inc.", Email = "pay@vixly.test", Tag = "VixlyPay", Phone = "+1 305 555 0100" }, CancellationToken.None)).Succeeded);
    }

    private string PayLink(PlatformInvoice invoice)
    {
        (string token, PlatformPaymentLink link) = _kit.Administration().IssueLink(PlatformPaymentLinkKind.PayInvoice, invoice.PlatformTenantId, invoice.Id, null);
        _kit.Payments.Links.Add(link);
        return token;
    }

    private Task<OpenPlatformPaymentLinkResponse> OpenAsync(string token)
    {
        return new OpenPlatformPaymentLinkUseCase(_kit.Administration(), _kit.ZelleAdministration()).ExecuteAsync(
            PlatformAnonymousAuthorizationContext.Instance, new OpenPlatformPaymentLinkRequest { Token = token }, CancellationToken.None);
    }

    private async Task<string?> ReportAsync(string token, decimal amount = 100m, string code = "ZL-1001")
    {
        var response = await new ReportPlatformZellePaymentUseCase(_kit.ZelleAdministration()).ExecuteAsync(PlatformAnonymousAuthorizationContext.Instance,
            new ReportPlatformZellePaymentRequest { Token = token, Amount = amount, SentOn = _kit.Now.UtcDateTime.Date, SenderName = "Grace Hopper", Reference = code }, CancellationToken.None);
        return response.Error?.Code;
    }

    private async Task DeliverAsync()
    {
        PlatformZelleNoticeHandler handler = new(_kit.ZelleAdministration(), _kit.Zelle);

        foreach (PlatformOutboxMessage message in _kit.Kit.Tenants.Tenants.Outbox.Where(item => item.Type == PlatformMessageTypes.ZelleNotice && item.ProcessedUtc is null).ToArray())
        {
            await handler.HandleAsync(message, CancellationToken.None);
            message.MarkProcessed(_kit.Now);
        }
    }

    [Fact]
    public async Task Settings_NeedThePaymentsPermissionAndStepUp_AndTheQrIsPublicOnlyWhileZelleIsOn()
    {
        UpdatePlatformZelleAccountUseCase update = new(_kit.ZelleAdministration());
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => update.ExecuteAsync(_kit.Kit.Staff(PlatformPermissions.BillingView),
            new UpdatePlatformZelleAccountRequest { Enabled = true, RecipientName = "Vixly Inc.", Email = "pay@vixly.test" }, CancellationToken.None));
        Assert.Equal("platform.payments.zelle_settings_invalid", (await update.ExecuteAsync(_kit.Kit.Staff(PlatformPermissions.PaymentsConfigure),
            new UpdatePlatformZelleAccountRequest { Enabled = true, RecipientName = "Vixly Inc." }, CancellationToken.None)).Error!.Code);

        var configure = _kit.Kit.Staff(PlatformPermissions.PaymentsConfigure);
        Assert.True((await new SetPlatformZelleQrCodeUseCase(_kit.ZelleAdministration()).ExecuteAsync(configure,
            new SetPlatformZelleQrCodeRequest { Content = Png, ContentType = "image/png" }, CancellationToken.None)).Succeeded);
        GetPlatformZelleQrCodeUseCase qr = new(_kit.ZelleAdministration());
        Assert.Equal("platform.payments.zelle_not_available", (await qr.ExecuteAsync(PlatformAnonymousAuthorizationContext.Instance, new GetPlatformZelleQrCodeRequest(), CancellationToken.None)).Error!.Code);

        await TurnOnAsync();
        var image = await qr.ExecuteAsync(PlatformAnonymousAuthorizationContext.Instance, new GetPlatformZelleQrCodeRequest(), CancellationToken.None);
        Assert.Equal("image/png", image.QrCode!.ContentType);
        Assert.Equal(1, image.QrCode.Version);
        Assert.Contains(_kit.Kit.Tenants.Store.AuditEntries, entry => entry.ActionCode == "platform.payments.zelle_updated");
    }

    [Fact]
    public async Task Checkout_OffersZelle_ForOpenUsDollarInvoicesOnly_WhileZelleIsOn()
    {
        (_, PlatformInvoice invoice) = _kit.OpenInvoice();
        string token = PayLink(invoice);
        Assert.Null((await OpenAsync(token)).Link!.Zelle);

        await TurnOnAsync();
        PlatformZelleOffer offer = (await OpenAsync(token)).Link!.Zelle!;
        Assert.Equal("Vixly Inc.", offer.RecipientName);
        Assert.Equal("VixlyPay", offer.Tag);
        Assert.True(offer.CanReport);
        Assert.Null(offer.QrCodeVersion);

        (_, PlatformInvoice euros) = _kit.OpenInvoice();
        typeof(PlatformInvoice).GetProperty(nameof(PlatformInvoice.CurrencyCode))!.SetValue(euros, "EUR");
        Assert.Null((await OpenAsync(PayLink(euros))).Link!.Zelle);
        Assert.Equal("platform.payments.zelle_not_available", await ReportAsync(PayLink(euros)));
    }

    [Fact]
    public async Task Report_NeverPays_OneWaitsAtATime_AndTheCustomerIsTold()
    {
        await TurnOnAsync();
        (PlatformTenant tenant, PlatformInvoice invoice) = _kit.OpenInvoice();
        string token = PayLink(invoice);

        Assert.Equal("platform.payments.zelle_amount_invalid", await ReportAsync(token, amount: 500m));
        Assert.Null(await ReportAsync(token));
        Assert.Equal(PlatformInvoiceStatus.Issued, invoice.Status);
        Assert.Equal(100m, invoice.Balance);
        Assert.Equal("platform.payments.zelle_claim_waiting", await ReportAsync(token, code: "ZL-1002"));

        PlatformZelleOffer offer = (await OpenAsync(token)).Link!.Zelle!;
        Assert.False(offer.CanReport);
        Assert.Equal(100m, offer.Waiting!.Amount);

        await DeliverAsync();
        (string to, PlatformZelleMail mail) = Assert.Single(_kit.Zelle.Mails);
        Assert.Equal(PlatformZelleNoticeKind.Received, mail.Kind);
        Assert.Equal(tenant.ContactFor(PlatformTenantContactRole.Billing).Email.Value, to);
    }

    [Fact]
    public async Task StaffConfirm_RecordsAZellePayment_WithTheCustomersCode()
    {
        await TurnOnAsync();
        (_, PlatformInvoice invoice) = _kit.OpenInvoice();
        Assert.Null(await ReportAsync(PayLink(invoice)));
        PlatformZelleClaim claim = Assert.Single(_kit.Zelle.Claims);
        ConfirmPlatformZelleClaimUseCase confirm = new(_kit.ZelleAdministration());

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => confirm.ExecuteAsync(_kit.Kit.Staff(PlatformPermissions.BillingView),
            new ConfirmPlatformZelleClaimRequest { ClaimId = claim.Id }, CancellationToken.None));
        Assert.Equal("platform.billing.amount_above_balance", (await confirm.ExecuteAsync(_kit.Kit.Staff(PlatformPermissions.BillingRecordPayments),
            new ConfirmPlatformZelleClaimRequest { ClaimId = claim.Id, ReceivedAmount = 150m }, CancellationToken.None)).Error!.Code);
        Assert.True(claim.IsWaiting);

        var done = await confirm.ExecuteAsync(_kit.Kit.Staff(PlatformPermissions.BillingRecordPayments),
            new ConfirmPlatformZelleClaimRequest { ClaimId = claim.Id, Note = "In the bank statement" }, CancellationToken.None);
        Assert.True(done.Succeeded, done.Error?.Code);
        Assert.Equal(PlatformInvoiceStatus.Paid, invoice.Status);
        PlatformPayment payment = Assert.Single(invoice.Payments);
        Assert.Equal(PlatformPaymentMethod.Zelle, payment.Method);
        Assert.Equal("ZL-1001", payment.Reference);
        Assert.Equal(PlatformZelleClaimStatus.Confirmed, claim.Status);
        Assert.Equal(payment.Id, claim.PlatformPaymentId);
        Assert.Contains(_kit.Kit.Tenants.Tenants.Outbox, item => item.Type == PlatformMessageTypes.InvoicePaid);
        Assert.Equal("platform.payments.zelle_claim_decided", (await confirm.ExecuteAsync(_kit.Kit.Staff(PlatformPermissions.BillingRecordPayments),
            new ConfirmPlatformZelleClaimRequest { ClaimId = claim.Id }, CancellationToken.None)).Error!.Code);
    }

    [Fact]
    public async Task StaffReject_TellsTheCustomerWhy_AndAfterThreeReportsTheCustomerMustContactVixly()
    {
        await TurnOnAsync();
        (_, PlatformInvoice invoice) = _kit.OpenInvoice();
        string token = PayLink(invoice);
        RejectPlatformZelleClaimUseCase reject = new(_kit.ZelleAdministration());
        var staff = _kit.Kit.Staff(PlatformPermissions.BillingRecordPayments);

        for (int round = 1; round <= PlatformZelleLimits.MaxClaimsPerInvoice; round++)
        {
            Assert.Null(await ReportAsync(token, code: "ZL-" + round));
            PlatformZelleClaim claim = _kit.Zelle.Claims.Single(item => item.IsWaiting);
            Assert.Equal("platform.payments.zelle_reason_required", (await reject.ExecuteAsync(staff, new RejectPlatformZelleClaimRequest { ClaimId = claim.Id }, CancellationToken.None)).Error!.Code);
            Assert.True((await reject.ExecuteAsync(staff, new RejectPlatformZelleClaimRequest { ClaimId = claim.Id, Reason = "No payment with this code reached us", Note = "Checked Chase" }, CancellationToken.None)).Succeeded);
        }

        Assert.Equal("platform.payments.zelle_too_many_claims", await ReportAsync(token, code: "ZL-9"));
        await DeliverAsync();
        PlatformZelleMail rejected = _kit.Zelle.Mails.Last().Mail;
        Assert.Equal(PlatformZelleNoticeKind.Rejected, rejected.Kind);
        Assert.Equal("No payment with this code reached us", rejected.Reason);
        Assert.DoesNotContain(_kit.Zelle.Mails, item => item.Mail.Reason == "Checked Chase");

        var waiting = await new ListPlatformZelleClaimsUseCase(_kit.ZelleAdministration()).ExecuteAsync(_kit.Kit.Staff(PlatformPermissions.BillingView),
            new ListPlatformZelleClaimsRequest { Status = PlatformZelleClaimStatus.Rejected }, CancellationToken.None);
        Assert.Equal(3, waiting.Claims!.TotalCount);
    }

    [Fact]
    public async Task WaitingReport_PausesCollections_UntilStaffDecide()
    {
        CollectionsTestKit collections = new();
        PaymentsTestKit kit = collections.Payments;
        var configure = kit.Kit.Staff(PlatformPermissions.PaymentsConfigure);
        await new UpdatePlatformZelleAccountUseCase(kit.ZelleAdministration()).ExecuteAsync(configure,
            new UpdatePlatformZelleAccountRequest { Enabled = true, RecipientName = "Vixly Inc.", Email = "pay@vixly.test" }, CancellationToken.None);
        (_, _, PlatformInvoice invoice) = collections.Overdue();
        collections.Day(1, invoice);
        await collections.RunAsync();
        PlatformCollectionCase collection = Assert.Single(collections.Store.Cases);

        (string token, PlatformPaymentLink link) = kit.Administration().IssueLink(PlatformPaymentLinkKind.PayInvoice, invoice.PlatformTenantId, invoice.Id, null);
        kit.Payments.Links.Add(link);
        var reported = await new ReportPlatformZellePaymentUseCase(kit.ZelleAdministration()).ExecuteAsync(PlatformAnonymousAuthorizationContext.Instance,
            new ReportPlatformZellePaymentRequest { Token = token, Amount = invoice.Balance, SentOn = kit.Now.UtcDateTime.Date, SenderName = "Grace", Reference = "ZL-77" }, CancellationToken.None);
        Assert.True(reported.Succeeded, reported.Error?.Code);

        collections.Day(14, invoice);
        await collections.RunAsync();
        Assert.True(collection.Held);
        Assert.NotEqual(PlatformCollectionStage.Blocked, collection.Stage);

        PlatformZelleClaim claim = kit.Zelle.Claims.Single();
        await new RejectPlatformZelleClaimUseCase(kit.ZelleAdministration()).ExecuteAsync(kit.Kit.Staff(PlatformPermissions.BillingRecordPayments),
            new RejectPlatformZelleClaimRequest { ClaimId = claim.Id, Reason = "Not received" }, CancellationToken.None);
        await collections.RunAsync();
        Assert.False(collection.Held);
    }
}
