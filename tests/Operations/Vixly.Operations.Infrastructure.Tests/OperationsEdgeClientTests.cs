using System.Net;
using System.Text.Json;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Vixly.Operations.Application.Branding;
using Vixly.Operations.Infrastructure.Edge;
using Vixly.Shared.Kernel.Security;

namespace Vixly.Operations.Infrastructure.Tests;

// =========================================================
// FILE: OperationsEdgeClientTests.cs
//
// PURPOSE:
// Proves Operations Web sends a company's brand to the Cloudflare Worker as the Worker expects (owner, 2026-10-28).
//
// RESPONSIBILITIES:
// - Signed POST to Edge:BrandUrl: slug, name, colour, language and the logo in base64
// - Not configured, or not https: nothing sent
// - A refusal throws; "stale" is not a failure
//
// IMPORTANT NOTES:
// - The Worker is a fake HTTP handler that records the request
// =========================================================
public sealed class OperationsEdgeClientTests
{
    private const string Secret = "0123456789abcdef0123456789abcdef0123456789abcdef0123456789abcdef";

    private const string Url = "https://api.vixly.ltd/__edge/brand";

    private static readonly DateTimeOffset Now = new(2026, 10, 29, 6, 0, 0, TimeSpan.Zero);

    private static readonly OperationsEdgeBrand Brand = new("galeo", "Galeo Logistics", "#0F4C5C", "fr", new byte[] { 137, 80, 78, 71 }, "image/png");

    [Fact]
    public async Task SendsTheBrand_Signed_ToTheWorker()
    {
        Capture capture = new(HttpStatusCode.OK, "{\"ok\":true}");

        Assert.Equal(OperationsEdgeSendResult.Sent, await Client(capture, Url).SendBrandAsync(Brand, CancellationToken.None));

        Assert.Equal(Url, capture.Address);
        Assert.Equal(VixlyEdgeSignature.Sign(Secret, "/__edge/brand", Now.ToUnixTimeMilliseconds(), capture.Body!), capture.Signature);
        using JsonDocument body = JsonDocument.Parse(capture.Body!);
        Assert.Equal("galeo", body.RootElement.GetProperty("slug").GetString());
        Assert.Equal("Galeo Logistics", body.RootElement.GetProperty("name").GetString());
        Assert.Equal("#0F4C5C", body.RootElement.GetProperty("colour").GetString());
        Assert.Equal("fr", body.RootElement.GetProperty("language").GetString());
        Assert.Equal("iVBORw==", body.RootElement.GetProperty("logo").GetString());
        Assert.Equal("image/png", body.RootElement.GetProperty("logoType").GetString());
    }

    [Theory]
    [InlineData("", Secret)]
    [InlineData(Url, "")]
    [InlineData("http://api.vixly.ltd/__edge/brand", Secret)]
    public async Task NotConfigured_SendsNothing(string url, string secret)
    {
        Capture capture = new(HttpStatusCode.OK, "{}");

        Assert.Equal(OperationsEdgeSendResult.NotConfigured, await Client(capture, url, secret).SendBrandAsync(Brand, CancellationToken.None));
        Assert.Null(capture.Address);
    }

    [Fact]
    public async Task ARefusalThrows_AndStaleIsNotAFailure()
    {
        await Assert.ThrowsAsync<HttpRequestException>(() => Client(new Capture(HttpStatusCode.BadRequest, "{\"error\":\"brand\"}"), Url).SendBrandAsync(Brand, CancellationToken.None));
        Assert.Equal(OperationsEdgeSendResult.Stale, await Client(new Capture(HttpStatusCode.OK, "{\"ok\":true,\"stale\":true}"), Url).SendBrandAsync(Brand, CancellationToken.None));
    }

    private static OperationsEdgeClient Client(Capture capture, string url, string secret = Secret) =>
        new(new HttpClient(capture), Options.Create(new OperationsEdgeOptions { BrandUrl = url, BrandSecret = secret }), new FixedClock(), NullLogger<OperationsEdgeClient>.Instance);

    private sealed class FixedClock : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => Now;
    }

    private sealed class Capture : HttpMessageHandler
    {
        private readonly HttpStatusCode _status;

        private readonly string _answer;

        public Capture(HttpStatusCode status, string answer)
        {
            _status = status;
            _answer = answer;
        }

        public string? Address { get; private set; }

        public string? Signature { get; private set; }

        public string? Body { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Address = request.RequestUri!.ToString();
            Signature = request.Headers.GetValues(VixlyEdgeSignature.SignatureHeader).Single();
            Body = await request.Content!.ReadAsStringAsync(cancellationToken);
            return new HttpResponseMessage(_status) { Content = new StringContent(_answer) };
        }
    }
}
