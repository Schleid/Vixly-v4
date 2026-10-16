using Vixly.Platform.Domain.Payments;
using Vixly.Shared.Kernel.Domain;

namespace Vixly.Platform.Domain.Tests;

// =========================================================
// FILE: PlatformZelleTests.cs
//
// PURPOSE:
// Proves the Zelle settings and claim rules (Slice 5 update).
//
// RESPONSIBILITIES:
// - Settings: recipient and one way to send before turning on; field checks; QR image checks and versions
// - Claims: USD only, amount up to the balance, recent date, sender and code; one decision only
//
// IMPORTANT NOTES:
// - Tests assert codes, never text (L-1)
// =========================================================
public sealed class PlatformZelleTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 16, 12, 0, 0, TimeSpan.Zero);

    private static readonly Guid Staff = Guid.NewGuid();

    private static readonly byte[] Png = { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0, 0, 0, 0 };

    private static string Code(Action action) => Assert.Throws<DomainException>(action).Error.Code;

    [Fact]
    public void Settings_NeedARecipientAndAWayToSend_BeforeZelleIsOn()
    {
        PlatformZelleAccount account = PlatformZelleAccount.Create(Now);

        Assert.Equal("platform.payments.zelle_settings_invalid", Code(() => account.Update(true, "Vixly Inc.", null, null, null, Staff, Now)));
        Assert.Equal("platform.payments.zelle_settings_invalid", Code(() => account.Update(true, " ", "pay@vixly.test", null, null, Staff, Now)));
        Assert.Equal("platform.payments.zelle_email_invalid", Code(() => account.Update(true, "Vixly Inc.", "not-an-email", null, null, Staff, Now)));
        Assert.Equal("platform.payments.zelle_tag_invalid", Code(() => account.Update(true, "Vixly Inc.", null, "has space", null, Staff, Now)));
        Assert.Equal("platform.payments.zelle_phone_invalid", Code(() => account.Update(true, "Vixly Inc.", null, null, "call me", Staff, Now)));

        account.Update(true, " Vixly Inc. ", " Pay@Vixly.test ", "VixlyPay", "+1 305 555 0100", Staff, Now);
        Assert.True(account.Enabled);
        Assert.Equal("Vixly Inc.", account.RecipientName);
        Assert.Equal("VixlyPay", account.Tag);
        Assert.NotNull(account.Phone);

        account.Update(false, null, null, null, null, Staff, Now);
        Assert.False(account.Enabled);
    }

    [Fact]
    public void QrCode_MustBeASmallPngOrJpeg_AndEachChangeBumpsTheVersion()
    {
        PlatformZelleAccount account = PlatformZelleAccount.Create(Now);

        Assert.Equal("platform.payments.zelle_qr_invalid", Code(() => account.SetQrCode(new byte[] { 1, 2, 3, 4, 5 }, "image/png", Staff, Now)));
        Assert.Equal("platform.payments.zelle_qr_invalid", Code(() => account.SetQrCode(Png, "image/gif", Staff, Now)));
        Assert.Equal("platform.payments.zelle_qr_invalid", Code(() => account.SetQrCode(Png.Concat(new byte[300 * 1024]).ToArray(), "image/png", Staff, Now)));

        account.SetQrCode(Png, "image/png", Staff, Now);
        Assert.True(account.HasQrCode);
        Assert.Equal(1, account.QrCodeVersion);

        account.SetQrCode(null, null, Staff, Now);
        Assert.False(account.HasQrCode);
        Assert.Equal(2, account.QrCodeVersion);
    }

    [Fact]
    public void Claim_IsUsdOnly_UpToTheBalance_Recent_WithSenderAndCode()
    {
        Guid tenant = Guid.NewGuid();
        Guid invoice = Guid.NewGuid();
        PlatformZelleClaim Submit(string currency = "USD", decimal amount = 100m, int daysBack = 1, string sender = "Grace Hopper", string code = "ZL123") =>
            PlatformZelleClaim.Submit(tenant, invoice, "INV-2026-000001", currency, 150m, amount, Now.UtcDateTime.Date.AddDays(-daysBack), sender, code, PlatformZelleClaimSource.PaymentLink, Now);

        Assert.Equal("platform.payments.zelle_not_available", Code(() => Submit(currency: "EUR")));
        Assert.Equal("platform.payments.zelle_amount_invalid", Code(() => Submit(amount: 0m)));
        Assert.Equal("platform.payments.zelle_amount_invalid", Code(() => Submit(amount: 150.01m)));
        Assert.Equal("platform.payments.zelle_amount_invalid", Code(() => Submit(amount: 10.005m)));
        Assert.Equal("platform.payments.zelle_date_invalid", Code(() => Submit(daysBack: 31)));
        Assert.Equal("platform.payments.zelle_date_invalid", Code(() => Submit(daysBack: -2)));
        Assert.Equal("platform.payments.zelle_sender_invalid", Code(() => Submit(sender: " ")));
        Assert.Equal("platform.payments.zelle_reference_invalid", Code(() => Submit(code: new string('x', 61))));

        PlatformZelleClaim claim = Submit(amount: 150m, daysBack: 0);
        Assert.Equal(PlatformZelleClaimStatus.Waiting, claim.Status);
        Assert.Equal("USD", claim.CurrencyCode);
    }

    [Fact]
    public void Claim_IsDecidedOnce_AndARejectionNeedsAReason()
    {
        PlatformZelleClaim claim = PlatformZelleClaim.Submit(Guid.NewGuid(), Guid.NewGuid(), "INV-2026-000002", "USD", 99m, 99m, Now.UtcDateTime.Date, "Grace", "ZL9", PlatformZelleClaimSource.Registration, Now);

        Assert.Equal("platform.payments.zelle_reason_required", Code(() => claim.Reject(" ", null, Staff, Now)));
        Assert.Equal("platform.payments.zelle_actor_required", Code(() => claim.Confirm(99m, Now, Guid.NewGuid(), null, Guid.Empty, Now)));

        Guid payment = Guid.NewGuid();
        claim.Confirm(99m, Now, payment, "Seen in the bank statement", Staff, Now);
        Assert.Equal(PlatformZelleClaimStatus.Confirmed, claim.Status);
        Assert.Equal(payment, claim.PlatformPaymentId);
        Assert.Equal("platform.payments.zelle_claim_decided", Code(() => claim.Reject("Not found", null, Staff, Now)));
    }
}
