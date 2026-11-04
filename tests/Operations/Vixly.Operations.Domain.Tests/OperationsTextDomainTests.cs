using Vixly.Operations.Domain.Texts;
using Vixly.Shared.Kernel.Domain;

namespace Vixly.Operations.Domain.Tests;

// =========================================================
// FILE: OperationsTextDomainTests.cs
//
// PURPOSE:
// Proves the text message rules (Slice 9).
//
// RESPONSIBILITIES:
// - The company's Twilio keys and senders are checked; only the token's last four are kept in clear
// - WhatsApp templates are approved content ids per notice
// - The log keeps only the end of the number
//
// IMPORTANT NOTES:
// - Pure domain; no database
// =========================================================
public sealed class OperationsTextDomainTests
{
    private static readonly Guid Tenant = Guid.NewGuid();

    private static readonly DateTimeOffset Now = new(2026, 10, 22, 12, 0, 0, TimeSpan.Zero);

    private static readonly string Sid = "AC" + new string('a', 32);

    private static readonly string Token = new string('b', 28) + "c0de";

    [Fact]
    public void Provider_ChecksKeysAndSenders_AndKeepsOnlyTheTokenHint()
    {
        Assert.Equal(OperationsTextErrors.KeysInvalid, Assert.Throws<DomainException>(() =>
            OperationsTextProvider.Connect(Tenant, "AC123", Token, "protected", null, "+13055550123", null, Guid.NewGuid(), Now)).Error);
        Assert.Equal(OperationsTextErrors.KeysInvalid, Assert.Throws<DomainException>(() =>
            OperationsTextProvider.Connect(Tenant, Sid, Token, " ", null, "+13055550123", null, Guid.NewGuid(), Now)).Error);
        Assert.Equal(OperationsTextErrors.SenderInvalid, Assert.Throws<DomainException>(() =>
            OperationsTextProvider.Connect(Tenant, Sid, Token, "protected", null, "3055550123", null, Guid.NewGuid(), Now)).Error);

        OperationsTextProvider provider = OperationsTextProvider.Connect(Tenant, Sid, Token, "protected", "  North  ", "+1 305 555 0123", null, Guid.NewGuid(), Now);
        Assert.Equal(("c0de", "North", "+13055550123"), (provider.TokenHint, provider.AccountName, provider.SmsFrom));
        Assert.True(provider.SendsSms);
        Assert.False(provider.SendsWhatsApp);

        // A messaging service id is a valid SMS sender; WhatsApp needs a number.
        provider.Senders("MG" + new string('1', 32), "+13055550199", Now);
        Assert.True(provider.SendsWhatsApp);
        Assert.Equal(OperationsTextErrors.SenderInvalid, Assert.Throws<DomainException>(() => provider.Senders(null, "MG" + new string('1', 32), Now)).Error);
    }

    [Fact]
    public void Templates_AreContentIdsPerNotice_AndBlanksAreDropped()
    {
        OperationsTextProvider provider = OperationsTextProvider.Connect(Tenant, Sid, Token, "protected", null, null, "+13055550199", Guid.NewGuid(), Now);
        string ready = "HX" + new string('2', 32);
        provider.ChangeTemplates(new Dictionary<OperationsTextNotices, string?> { [OperationsTextNotices.ReadyForPickup] = ready, [OperationsTextNotices.PickupCode] = " " }, Now);
        Assert.Equal(ready, provider.Template(OperationsTextNotices.ReadyForPickup));
        Assert.Null(provider.Template(OperationsTextNotices.PickupCode));
        Assert.Equal(OperationsTextErrors.TemplateInvalid, Assert.Throws<DomainException>(() =>
            provider.ChangeTemplates(new Dictionary<OperationsTextNotices, string?> { [OperationsTextNotices.ReadyForPickup] = "hello" }, Now)).Error);
        Assert.Equal(OperationsTextErrors.TemplateInvalid, Assert.Throws<DomainException>(() =>
            provider.ChangeTemplates(new Dictionary<OperationsTextNotices, string?> { [OperationsTextNotices.ReadyForPickup | OperationsTextNotices.PickupCode] = ready }, Now)).Error);
        Assert.Equal(ready, provider.Template(OperationsTextNotices.ReadyForPickup));
    }

    [Fact]
    public void Log_KeepsOnlyTheEndOfTheNumber()
    {
        OperationsTextMessage message = OperationsTextMessage.Record(Tenant, Guid.NewGuid(), OperationsNoticeChannel.Sms, OperationsTextNotices.ReadyForPickup, "+13055550123",
            OperationsTextStatus.Sent, "SM1", null, Now);
        Assert.Equal("…0123", message.ToHint);

        // Twilio's later reports: delivered stays delivered; a refused line never moves.
        Assert.False(message.Report(OperationsTextStatus.Sent, null, Now));
        Assert.True(message.Report(OperationsTextStatus.Delivered, null, Now));
        Assert.False(message.Report(OperationsTextStatus.Undelivered, "30003", Now));
        Assert.Equal(OperationsTextStatus.Delivered, message.Status);

        OperationsTextMessage refused = OperationsTextMessage.Record(Tenant, Guid.NewGuid(), OperationsNoticeChannel.Sms, OperationsTextNotices.ReadyForPickup, "+13055550123",
            OperationsTextStatus.Failed, null, "21211", Now);
        Assert.False(refused.Report(OperationsTextStatus.Delivered, null, Now));

        OperationsTextMessage lost = OperationsTextMessage.Record(Tenant, Guid.NewGuid(), OperationsNoticeChannel.WhatsApp, OperationsTextNotices.ReadyForPickup, "+13055550123",
            OperationsTextStatus.Sent, "MM1", null, Now);
        Assert.True(lost.Report(OperationsTextStatus.Undelivered, "63016", Now));
        Assert.Equal((OperationsTextStatus.Undelivered, "63016"), (lost.Status, lost.ErrorCode));
    }

    [Fact]
    public void ANewPhoneNumber_TakesTheCustomerBackToEmail()
    {
        Vixly.Operations.Domain.Customers.OperationsCustomer customer = Vixly.Operations.Domain.Customers.OperationsCustomer.SignUp(Tenant, "Ana", "Pierre", null,
            Vixly.Shared.Kernel.ValueObjects.EmailAddress.Create("ana@example.com"), "+50937123456", Vixly.Shared.Kernel.Localization.LanguageCode.En, "$2a$12$hash", Now);
        customer.ChangeNoticeChannel(OperationsNoticeChannel.Sms, Now);
        customer.ChangeProfile("Ana", "Pierre", null, "+50937123456", Vixly.Shared.Kernel.Localization.LanguageCode.Fr, Now);
        Assert.Equal(OperationsNoticeChannel.Sms, customer.NoticeChannel);
        customer.ChangeProfile("Ana", "Pierre", null, "+50937999999", Vixly.Shared.Kernel.Localization.LanguageCode.Fr, Now);
        Assert.Equal(OperationsNoticeChannel.Email, customer.NoticeChannel);
    }

    [Fact]
    public void AnotherAccount_DropsTheOldTemplates_AndTheCallbackIsHttpsOnly()
    {
        OperationsTextProvider provider = OperationsTextProvider.Connect(Tenant, Sid, Token, "protected", null, null, "+13055550199", Guid.NewGuid(), Now);
        provider.ChangeTemplates(new Dictionary<OperationsTextNotices, string?> { [OperationsTextNotices.ReadyForPickup] = "HX" + new string('2', 32) }, Now);
        provider.Change(Sid, Token, "protected2", null, null, "+13055550199", Guid.NewGuid(), Now);
        Assert.Single(provider.Templates);
        provider.Change("AC" + new string('d', 32), Token, "protected3", null, null, "+13055550199", Guid.NewGuid(), Now);
        Assert.Empty(provider.Templates);

        provider.ReportTo("http://north.example/hooks/twilio", Now);
        Assert.Null(provider.CallbackUrl);
        provider.ReportTo("https://north.example/hooks/twilio", Now);
        Assert.Equal("https://north.example/hooks/twilio", provider.CallbackUrl);
    }
}
