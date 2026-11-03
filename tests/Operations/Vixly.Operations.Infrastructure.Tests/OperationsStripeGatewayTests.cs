using System.Net;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Logging.Abstractions;
using Vixly.Operations.Application.Ports;
using Vixly.Operations.Infrastructure.Payments;

namespace Vixly.Operations.Infrastructure.Tests;

// =========================================================
// FILE: OperationsStripeGatewayTests.cs
//
// PURPOSE:
// Proves the Stripe gateway's rules without the network (Slice 5F step 4).
//
// RESPONSIBILITIES:
// - A webhook is trusted only with the right secret, recent, over the exact payload
// - Amounts go in Stripe's smallest unit; a page carries our charge, the amount and an idempotency key
// - Stripe's refusals and outages become the two exceptions, without the key in the message
//
// IMPORTANT NOTES:
// - No database, no network: a handler answers as Stripe would
// =========================================================
public sealed class OperationsStripeGatewayTests
{
    private const string Secret = "whsec_test_0123456789abcdef0123";

    private static readonly DateTimeOffset Now = new(2026, 10, 22, 4, 0, 0, TimeSpan.Zero);

    // 2026-11-03: a company creates its webhook today in Stripe's newest format (2026-10-28.endive), while calls pin 2024-06-20.
    // The fields Vixly reads are the same in both; these are trimmed copies of real endive events.
    [Theory]
    [InlineData("checkout.session.completed",
        "{\"id\":\"cs_test_a1\",\"object\":\"checkout.session\",\"adaptive_pricing\":{\"enabled\":true},\"allowed_payment_method_types\":[\"card\"],\"amount_subtotal\":287900,"
        + "\"amount_total\":287900,\"branding_settings\":{\"display_name\":\"Galeo\"},\"currency\":\"usd\",\"livemode\":false,\"metadata\":{\"vixly_charge\":\"6f9619ff-8b86-d011-b42d-00c04fc964ff\"},"
        + "\"mode\":\"payment\",\"payment_intent\":\"pi_3UX\",\"payment_method_types\":[\"card\"],\"payment_status\":\"paid\",\"status\":\"complete\",\"ui_mode\":\"hosted_page\",\"wallet_options\":null}")]
    [InlineData("payment_intent.succeeded",
        "{\"id\":\"pi_3UX\",\"object\":\"payment_intent\",\"allowed_payment_method_types\":[\"card\"],\"amount\":287900,\"amount_details\":{\"tip\":{}},\"amount_received\":287900,"
        + "\"capture_method\":\"automatic_async\",\"currency\":\"usd\",\"livemode\":false,\"metadata\":{\"vixly_charge\":\"6f9619ff-8b86-d011-b42d-00c04fc964ff\"},"
        + "\"payment_details\":{\"order_reference\":\"cs_test_a1\"},\"payment_method_types\":[\"card\"],\"status\":\"succeeded\"}")]
    public void Webhook_InTheNewestStripeFormat_IsRead(string type, string item)
    {
        string payload = "{\"id\":\"evt_1\",\"object\":\"event\",\"api_version\":\"2026-10-28.endive\",\"context\":\"acct_1\",\"created\":1793667777,\"data\":{\"object\":" + item
            + "},\"livemode\":false,\"pending_webhooks\":1,\"request\":{\"id\":null},\"type\":\"" + type + "\"}";
        OperationsStripeGateway gateway = Gateway(new Answer(HttpStatusCode.OK, "{}"));

        OperationsCardEvent? read = gateway.ReadEvent(payload, Header(payload, Secret, Now), Secret, Now);

        Assert.NotNull(read);
        Assert.Equal(new Guid("6f9619ff-8b86-d011-b42d-00c04fc964ff"), read.ChargeId);
        Assert.Equal(new OperationsCardState(true, false, "pi_3UX", 2879m, "USD") { Live = false }, read.State);
    }

    // 2026-11-03: a restricted key without a permission is refused with 403; a full key reads everything.
    [Fact]
    public async Task MissingPermissions_NamesWhatARestrictedKeyCannotReach()
    {
        Assert.Equal(new[] { "checkout_sessions", "payment_intents", "refunds" },
            await Gateway(new Answer(HttpStatusCode.Forbidden, "{\"error\":{\"type\":\"invalid_request_error\"}}")).MissingPermissionsAsync("rk_test_1", CancellationToken.None));
        Assert.Empty(await Gateway(new Answer(HttpStatusCode.OK, "{\"object\":\"list\",\"data\":[]}")).MissingPermissionsAsync("rk_test_1", CancellationToken.None));
    }

    [Fact]
    public void Webhook_IsTrusted_OnlySignedRight_AndRecent()
    {
        const string payload = "{\"type\":\"payment_intent.succeeded\",\"data\":{\"object\":{\"id\":\"pi_1\",\"status\":\"succeeded\",\"amount_received\":1250,\"currency\":\"usd\","
            + "\"metadata\":{\"vixly_charge\":\"6f9619ff-8b86-d011-b42d-00c04fc964ff\"}}}}";
        OperationsStripeGateway gateway = Gateway(new Answer(HttpStatusCode.OK, "{}"));

        OperationsCardEvent? read = gateway.ReadEvent(payload, Header(payload, Secret, Now), Secret, Now);
        Assert.NotNull(read);
        Assert.Equal(new Guid("6f9619ff-8b86-d011-b42d-00c04fc964ff"), read.ChargeId);
        Assert.Equal(new OperationsCardState(true, false, "pi_1", 12.50m, "USD"), read.State);

        Assert.Null(gateway.ReadEvent(payload, Header(payload, "whsec_other_0123456789abcdef", Now), Secret, Now));
        Assert.Null(gateway.ReadEvent(payload + " ", Header(payload, Secret, Now), Secret, Now));
        Assert.Null(gateway.ReadEvent(payload, Header(payload, Secret, Now.AddMinutes(-6)), Secret, Now));
        Assert.Null(gateway.ReadEvent(payload, "t=1,v1=zz", Secret, Now));
        Assert.Null(gateway.ReadEvent(payload, null, Secret, Now));
    }

    [Fact]
    public void Amounts_GoInTheSmallestUnit()
    {
        Assert.Equal("1250", OperationsStripeGateway.Minor(12.50m, "USD"));
        Assert.Equal("1250", OperationsStripeGateway.Minor(1250m, "JPY"));
        Assert.Equal("12500", OperationsStripeGateway.Minor(12.5m, "KWD"));
        Assert.Equal(12.50m, OperationsStripeGateway.Major(1250, "USD"));
        Assert.Equal(1250m, OperationsStripeGateway.Major(1250, "JPY"));
    }

    [Fact]
    public async Task Page_CarriesTheCharge_TheAmount_AndAnIdempotencyKey()
    {
        Answer stripe = new(HttpStatusCode.OK, "{\"id\":\"cs_test_1\",\"url\":\"https://checkout.stripe.com/c/pay/cs_test_1\"}");
        Guid charge = Guid.NewGuid();

        OperationsCardPage page = await Gateway(stripe).CreatePageAsync("sk_test_key", new OperationsCardPageRequest(charge, 12.50m, "USD", "INV-1", "ana@example.com",
            "https://acme.example/staff/cashier/card/" + charge), CancellationToken.None);

        Assert.Equal(new OperationsCardPage("cs_test_1", "https://checkout.stripe.com/c/pay/cs_test_1"), page);
        Assert.Equal("Bearer sk_test_key", stripe.Authorization);
        Assert.Equal("page-" + charge.ToString("D"), stripe.IdempotencyKey);
        Assert.Contains("line_items%5B0%5D%5Bprice_data%5D%5Bunit_amount%5D=1250", stripe.Body, StringComparison.Ordinal);
        Assert.Contains("metadata%5Bvixly_charge%5D=" + charge.ToString("D"), stripe.Body, StringComparison.Ordinal);
        Assert.Contains("payment_intent_data%5Bmetadata%5D%5Bvixly_charge%5D=" + charge.ToString("D"), stripe.Body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Refusals_AndOutages_AreTold_WithoutTheKey()
    {
        OperationsCardRefusedException refused = await Assert.ThrowsAsync<OperationsCardRefusedException>(() =>
            Gateway(new Answer(HttpStatusCode.Unauthorized, "{\"error\":{\"code\":\"api_key_invalid\"}}")).CheckAsync("sk_test_secretvalue", CancellationToken.None));
        Assert.DoesNotContain("secretvalue", refused.Message, StringComparison.Ordinal);
        await Assert.ThrowsAsync<OperationsCardUnavailableException>(() =>
            Gateway(new Answer(HttpStatusCode.BadGateway, "oops")).GetPageAsync("sk_test_key", "cs_1", CancellationToken.None));

        // A restricted key that may not read the account still works.
        Assert.Equal(string.Empty, await Gateway(new Answer(HttpStatusCode.Forbidden, "{}")).CheckAsync("rk_test_key", CancellationToken.None));
    }

    private static string Header(string payload, string secret, DateTimeOffset at)
    {
        string stamp = at.ToUnixTimeSeconds().ToString(System.Globalization.CultureInfo.InvariantCulture);
        return "t=" + stamp + ",v1=" + Convert.ToHexStringLower(HMACSHA256.HashData(Encoding.UTF8.GetBytes(secret), Encoding.UTF8.GetBytes(stamp + "." + payload)));
    }

    private static OperationsStripeGateway Gateway(Answer answer) => new(new Factory(answer), NullLogger<OperationsStripeGateway>.Instance);

    private sealed class Factory(Answer answer) : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => new(answer, disposeHandler: false) { BaseAddress = new Uri("https://api.stripe.com/") };
    }

    private sealed class Answer(HttpStatusCode status, string body) : HttpMessageHandler
    {
        public string? Authorization { get; private set; }

        public string? IdempotencyKey { get; private set; }

        public string Body { get; private set; } = string.Empty;

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Authorization = request.Headers.Authorization?.ToString();
            IdempotencyKey = request.Headers.TryGetValues("Idempotency-Key", out IEnumerable<string>? keys) ? keys.Single() : null;
            Body = request.Content is null ? string.Empty : await request.Content.ReadAsStringAsync(cancellationToken);
            return new HttpResponseMessage(status) { Content = new StringContent(body) };
        }
    }
}
