using System.Net;
using Vixly.Operations.Domain.Api;
using Vixly.Shared.Kernel.Domain;

namespace Vixly.Operations.Domain.Tests;

// =========================================================
// FILE: OperationsApiDomainTests.cs
//
// PURPOSE:
// Proves the company API and webhook rules (Slice 10).
//
// RESPONSIBILITIES:
// - Keys: named, revoked once, last use noted at most once a minute
// - Webhook addresses: HTTPS to public hosts only; events chosen; places per event kind; off after three days of failures, on again from now
// - Deliveries: tried again on a schedule, then failed; sent again by hand
//
// IMPORTANT NOTES:
// - Pure domain; no database
// =========================================================
public sealed class OperationsApiDomainTests
{
    private static readonly Guid Tenant = Guid.NewGuid();

    private static readonly DateTimeOffset Now = new(2026, 10, 22, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Key_IsNamed_RevokedOnce_AndItsUseNotedAtMostOnceAMinute()
    {
        Assert.Equal(OperationsApiErrors.NameRequired, Assert.Throws<DomainException>(() =>
            OperationsApiKey.Create(Tenant, "  ", "0123456789abcdef", new string('a', 64), "abcd", Guid.NewGuid(), Now)).Error);
        OperationsApiKey key = OperationsApiKey.Create(Tenant, " ERP ", "0123456789abcdef", new string('a', 64), "abcd", Guid.NewGuid(), Now);
        Assert.Equal("ERP", key.Name);
        Assert.True(key.Used(Now));
        Assert.False(key.Used(Now.AddSeconds(30)));
        Assert.True(key.Used(Now.AddMinutes(2)));
        Guid by = Guid.NewGuid();
        key.Revoke(by, Now);
        key.Revoke(Guid.NewGuid(), Now.AddDays(1));
        Assert.Equal((false, by, Now), (key.Active, key.RevokedByUserId, key.RevokedUtc!.Value));
    }

    [Theory]
    [InlineData("http://hooks.example.com/in")]
    [InlineData("https://localhost/in")]
    [InlineData("https://intranet/in")]
    [InlineData("https://printer.local/in")]
    [InlineData("https://10.1.2.3/in")]
    [InlineData("https://172.20.0.1/in")]
    [InlineData("https://192.168.1.10/in")]
    [InlineData("https://127.0.0.1/in")]
    [InlineData("https://169.254.169.254/latest/meta-data")]
    [InlineData("https://100.64.0.1/in")]
    [InlineData("https://[::1]/in")]
    [InlineData("https://[fd00::1]/in")]
    [InlineData("https://user:pass@hooks.example.com/in")]
    [InlineData("https://hooks.example.com/in#part")]
    [InlineData("ftp://hooks.example.com/in")]
    [InlineData("https://hooks.example.com:22/in")]
    [InlineData("https://[64:ff9b::a9fe:a9fe]/in")]
    [InlineData("https://[2002:a00:1::1]/in")]
    [InlineData("")]
    public void Address_MustBeHttpsToAPublicHost(string url)
    {
        Assert.Equal(OperationsApiErrors.UrlInvalid, Assert.Throws<DomainException>(() => OperationsWebhookEndpoint.CheckUrl(url)).Error);
    }

    [Theory]
    [InlineData("8.8.8.8", true)]
    [InlineData("203.0.113.9", true)]
    [InlineData("10.0.0.1", false)]
    [InlineData("0.0.0.0", false)]
    [InlineData("224.0.0.1", false)]
    [InlineData("198.18.0.1", false)]
    [InlineData("::ffff:192.168.0.1", false)]
    [InlineData("2001:4860:4860::8888", true)]
    [InlineData("fe80::1", false)]
    [InlineData("64:ff9b::a9fe:a9fe", false)]
    [InlineData("::a00:1", false)]
    [InlineData("2002:c0a8:101::1", false)]
    [InlineData("100::1", false)]
    public void Connection_GoesOnlyToPublicAddresses(string address, bool expected)
    {
        Assert.Equal(expected, OperationsWebhookEndpoint.IsPublic(IPAddress.Parse(address)));
    }

    [Fact]
    public void Endpoint_KeepsAPlacePerEventKind_TurnsOffAfterThreeDaysOfFailures_AndStartsFromNowWhenOnAgain()
    {
        Assert.Equal(OperationsApiErrors.EventsRequired, Assert.Throws<DomainException>(() =>
            OperationsWebhookEndpoint.Create(Tenant, "https://hooks.example.com/in", OperationsWebhookEvents.None, "protected", "abcd", Guid.NewGuid(), 100, Now)).Error);
        OperationsWebhookEndpoint endpoint = OperationsWebhookEndpoint.Create(Tenant, "https://hooks.example.com:8443/in", OperationsWebhookEvents.Tracking, "protected", "abcd",
            Guid.NewGuid(), 100, Now);
        Assert.Equal(100, endpoint.Cursor(OperationsWebhookEvents.Tracking));
        endpoint.Advance(OperationsWebhookEvents.Tracking, 150);
        endpoint.Change("https://hooks.example.com/in", OperationsWebhookEvents.Tracking | OperationsWebhookEvents.InvoiceIssued, 200, Now.AddHours(1));
        Assert.Equal((150L, 200L), (endpoint.Cursor(OperationsWebhookEvents.Tracking), endpoint.Cursor(OperationsWebhookEvents.InvoiceIssued)));

        Assert.False(endpoint.Failed(Now));
        Assert.False(endpoint.Failed(Now.AddDays(2)));
        endpoint.Delivered(Now.AddDays(2).AddHours(1));
        Assert.False(endpoint.Failed(Now.AddDays(3)));
        Assert.True(endpoint.Failed(Now.AddDays(6)));
        Assert.Equal((false, true), (endpoint.Active, endpoint.DisabledForFailures));

        endpoint.Enable(900, Now.AddDays(7));
        Assert.Equal((true, false, (DateTimeOffset?)null), (endpoint.Active, endpoint.DisabledForFailures, endpoint.FailingSinceUtc));
        Assert.Equal(900, endpoint.Cursor(OperationsWebhookEvents.Tracking));
    }

    [Fact]
    public void Delivery_IsTriedAgainOnASchedule_ThenFails_AndCanBeSentAgain()
    {
        OperationsWebhookDelivery delivery = OperationsWebhookDelivery.Create(Tenant, Guid.NewGuid(), "evt_1", "customer.created", "{}", Now);
        Assert.True(delivery.Due(Now));
        delivery.Lease(Guid.NewGuid(), Now.AddMinutes(2), Now);
        Assert.False(delivery.Due(Now.AddMinutes(1)));
        DateTimeOffset at = Now;

        for (int attempt = 1; attempt <= OperationsWebhookDelivery.Backoff.Count; attempt++)
        {
            Assert.False(delivery.Attempted(500, "status", at));
            Assert.Equal(at + OperationsWebhookDelivery.Backoff[attempt - 1], delivery.NextAttemptUtc);
            at = delivery.NextAttemptUtc;
        }

        Assert.True(OperationsWebhookDelivery.Backoff.Aggregate(TimeSpan.Zero, (sum, wait) => sum + wait) < TimeSpan.FromDays(2));
        Assert.True(delivery.Attempted(null, "timeout", at));
        Assert.Equal((OperationsWebhookStatus.Failed, 9), (delivery.Status, delivery.Attempts));
        Assert.False(delivery.Due(at.AddDays(1)));

        delivery.Resend(at);
        Assert.Equal((OperationsWebhookStatus.Pending, 0), (delivery.Status, delivery.Attempts));
        delivery.Succeeded(204, at);
        Assert.Equal((OperationsWebhookStatus.Delivered, 204), (delivery.Status, delivery.LastStatusCode!.Value));

        // A test is tried once.
        OperationsWebhookDelivery test = OperationsWebhookDelivery.Create(Tenant, Guid.NewGuid(), "evt_test_1", OperationsWebhookEventNames.Test, "{}", Now);
        Assert.True(test.Attempted(503, "status", Now));
        Assert.Equal(OperationsWebhookStatus.Failed, test.Status);
    }
}
