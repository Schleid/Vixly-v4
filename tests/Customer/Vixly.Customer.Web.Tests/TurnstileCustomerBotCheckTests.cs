using System.Net;
using Microsoft.Extensions.Options;
using Vixly.Customer.Web.Portal;

namespace Vixly.Customer.Web.Tests;

// =========================================================
// FILE: TurnstileCustomerBotCheckTests.cs
//
// PURPOSE:
// Proves the sign-up bot check (Slice 2, ADR-0019).
//
// RESPONSIBILITIES:
// - No token, no secret or an outage fails closed; a success answer passes
// - Turned off, it lets everyone through
//
// IMPORTANT NOTES:
// - The verify address is a fake handler
// =========================================================
public sealed class TurnstileCustomerBotCheckTests
{
    [Fact]
    public async Task MissingToken_OrSecret_Fails_WithoutAsking()
    {
        Answering handler = new(HttpStatusCode.OK, "{\"success\":true}");

        Assert.False(await Check(handler, new CustomerBotCheckOptions { SecretKey = "s" }).VerifyAsync(null, null, CancellationToken.None));
        Assert.False(await Check(handler, new CustomerBotCheckOptions { SecretKey = "" }).VerifyAsync("token", null, CancellationToken.None));
        Assert.Equal(0, handler.Calls);
    }

    [Theory]
    [InlineData(HttpStatusCode.OK, "{\"success\":true}", true)]
    [InlineData(HttpStatusCode.OK, "{\"success\":false}", false)]
    [InlineData(HttpStatusCode.ServiceUnavailable, "", false)]
    [InlineData(HttpStatusCode.OK, "not json", false)]
    public async Task TheAnswer_Decides(HttpStatusCode status, string body, bool expected)
    {
        Answering handler = new(status, body);

        Assert.Equal(expected, await Check(handler, new CustomerBotCheckOptions { SecretKey = "s" }).VerifyAsync("token", "203.0.113.5", CancellationToken.None));
        Assert.Contains("remoteip=203.0.113.5", handler.LastBody, StringComparison.Ordinal);
    }

    [Fact]
    public async Task TurnedOff_LetsEveryoneThrough()
    {
        Assert.True(await Check(new Answering(HttpStatusCode.OK, "{}"), new CustomerBotCheckOptions { Enabled = false }).VerifyAsync(null, null, CancellationToken.None));
    }

    private static TurnstileCustomerBotCheck Check(Answering handler, CustomerBotCheckOptions options) =>
        new(new Factory(handler), Options.Create(options));

    private sealed class Factory(HttpMessageHandler handler) : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => new(handler, false);
    }

    private sealed class Answering(HttpStatusCode status, string body) : HttpMessageHandler
    {
        public int Calls { get; private set; }

        public string LastBody { get; private set; } = string.Empty;

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Calls++;
            LastBody = request.Content is null ? string.Empty : await request.Content.ReadAsStringAsync(cancellationToken);
            return new HttpResponseMessage(status) { Content = new StringContent(body, System.Text.Encoding.UTF8, "application/json") };
        }
    }
}
