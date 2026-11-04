using System.Net;
using System.Text.Json;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Vixly.Platform.Application.Maintenance;
using Vixly.Platform.Infrastructure.Edge;
using Vixly.Shared.Kernel.Security;

namespace Vixly.Platform.Infrastructure.Tests;

// =========================================================
// FILE: PlatformEdgeClientTests.cs
//
// PURPOSE:
// Proves Control sends the maintenance windows to the Cloudflare Worker as the Worker expects (owner, 2026-10-28).
//
// RESPONSIBILITIES:
// - Signed POST of the whole list to Edge:MaintenanceUrl; the pass hash, never a pass link
// - Not configured, or not https: nothing sent
// - A refusal throws; "stale" is not a failure
//
// IMPORTANT NOTES:
// - The Worker is a fake HTTP handler that records the request
// =========================================================
public sealed class PlatformEdgeClientTests
{
    private const string Secret = "0123456789abcdef0123456789abcdef0123456789abcdef0123456789abcdef";

    private static readonly DateTimeOffset Now = new(2026, 10, 29, 3, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task SendsTheWholeList_Signed_ToTheWorker()
    {
        Capture capture = new(HttpStatusCode.OK, "{\"ok\":true}");
        Guid id = Guid.NewGuid();
        PlatformEdgeWindowList list = new(new[]
        {
            new PlatformEdgeWindow(id, Now.AddMinutes(5), Now.AddHours(2), new[] { "staff", "customers" },
                new Dictionary<string, string> { ["en"] = "Scanners" }, new string('a', 64))
        });

        PlatformEdgeSendResult result = await Client(capture, "https://api.vixly.ltd/__edge/maintenance").SendMaintenanceAsync(list, CancellationToken.None);

        Assert.Equal(PlatformEdgeSendResult.Sent, result);
        Assert.Equal("https://api.vixly.ltd/__edge/maintenance", capture.Address);
        Assert.Equal(Now.ToUnixTimeMilliseconds().ToString(System.Globalization.CultureInfo.InvariantCulture), capture.Time);
        Assert.Equal(VixlyEdgeSignature.Sign(Secret, "/__edge/maintenance", Now.ToUnixTimeMilliseconds(), capture.Body!), capture.Signature);

        using JsonDocument body = JsonDocument.Parse(capture.Body!);
        JsonElement window = Assert.Single(body.RootElement.GetProperty("windows").EnumerateArray());
        Assert.Equal(id, window.GetProperty("id").GetGuid());
        Assert.Equal("2026-10-29T03:05:00.0000000Z", window.GetProperty("startsUtc").GetString());
        Assert.Equal(new[] { "staff", "customers" }, window.GetProperty("sites").EnumerateArray().Select(item => item.GetString()).ToArray());
        Assert.Equal("Scanners", window.GetProperty("messages").GetProperty("en").GetString());
        Assert.Equal(new string('a', 64), window.GetProperty("passHash").GetString());
    }

    [Theory]
    [InlineData("", Secret)]
    [InlineData("https://api.vixly.ltd/__edge/maintenance", "")]
    [InlineData("https://api.vixly.ltd/__edge/maintenance", "too-short")]
    [InlineData("http://api.vixly.ltd/__edge/maintenance", Secret)]
    public async Task NotConfigured_SendsNothing(string url, string secret)
    {
        Capture capture = new(HttpStatusCode.OK, "{}");

        PlatformEdgeSendResult result = await Client(capture, url, secret).SendMaintenanceAsync(new PlatformEdgeWindowList(Array.Empty<PlatformEdgeWindow>()), CancellationToken.None);

        Assert.Equal(PlatformEdgeSendResult.NotConfigured, result);
        Assert.Null(capture.Address);
    }

    [Fact]
    public async Task ARefusalThrows_AndStaleIsNotAFailure()
    {
        PlatformEdgeWindowList empty = new(Array.Empty<PlatformEdgeWindow>());

        await Assert.ThrowsAsync<HttpRequestException>(() => Client(new Capture(HttpStatusCode.Unauthorized, "{\"error\":\"signature\"}"), "https://api.vixly.ltd/__edge/maintenance")
            .SendMaintenanceAsync(empty, CancellationToken.None));
        Assert.Equal(PlatformEdgeSendResult.Stale, await Client(new Capture(HttpStatusCode.OK, "{\"ok\":true,\"stale\":true}"), "https://api.vixly.ltd/__edge/maintenance")
            .SendMaintenanceAsync(empty, CancellationToken.None));
    }

    private static PlatformEdgeClient Client(Capture capture, string url, string secret = Secret) =>
        new(new HttpClient(capture), Options.Create(new PlatformEdgeOptions { MaintenanceUrl = url, MaintenanceSecret = secret }), new FixedTimeProvider(Now), NullLogger<PlatformEdgeClient>.Instance);

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

        public string? Time { get; private set; }

        public string? Signature { get; private set; }

        public string? Body { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Address = request.RequestUri!.ToString();
            Time = request.Headers.GetValues(VixlyEdgeSignature.TimeHeader).Single();
            Signature = request.Headers.GetValues(VixlyEdgeSignature.SignatureHeader).Single();
            Body = await request.Content!.ReadAsStringAsync(cancellationToken);
            return new HttpResponseMessage(_status) { Content = new StringContent(_answer) };
        }
    }
}
