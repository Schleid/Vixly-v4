using System.Net;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Vixly.Platform.Application.SalesFeed;
using Vixly.Platform.Infrastructure.SalesFeed;

namespace Vixly.Platform.Infrastructure.Tests;

// =========================================================
// FILE: PlatformSalesFeedClientTests.cs
//
// PURPOSE:
// Proves the signed POST to TechOS and how its answers are read (contract version 1).
//
// RESPONSIBILITIES:
// - Headers X-TC-Source, X-TC-Contract, X-TC-Timestamp, X-TC-Signature (v1=hex HMAC-SHA256 of "timestamp.body")
// - 200 lists; 413 too large; 400/401 refused; 404, 5xx and no answer retried
// - Off without an https address, a secret or a source
//
// IMPORTANT NOTES:
// - No network: a fake handler answers
// =========================================================
public sealed class PlatformSalesFeedClientTests
{
    private const string Secret = "tc-shared-secret";

    private const string Url = "https://techcrafters.dyndns.ws/api/sales-sources/vixly/events";

    private static readonly DateTimeOffset Now = DateTimeOffset.FromUnixTimeSeconds(1793664000);

    [Fact]
    public void Signature_IsLowerCaseHexHmacOfTimestampDotBody()
    {
        Assert.Equal("v1=9ac7cbd82478f4deaf246b69a92edd6640b290cd708e1efdc27f3432dd8fc1e8", PlatformSalesFeedSignature.Sign(Secret, "1793664000", "{\"events\":[]}"));
        Assert.Equal("1793664000", PlatformSalesFeedSignature.Timestamp(Now));
    }

    [Fact]
    public async Task Post_IsSigned_WithTheTimeOfTheAttempt_AndTheBodyUnchanged()
    {
        Handler handler = new(HttpStatusCode.OK, "{\"accepted\":[],\"duplicates\":[],\"rejected\":[]}");
        string body = "{\"events\":[{\"id\":\"x\",\"tenant\":{\"phone\":\"+15145550100\"}}]}";

        PlatformSalesFeedAnswer answer = await Client(handler).SendAsync(body, CancellationToken.None);

        Assert.Equal(PlatformSalesFeedAnswerKind.Read, answer.Kind);
        Assert.Equal(Url, handler.Address);
        Assert.Equal(body, handler.Body);
        Assert.Equal("vixly", handler.Headers["X-TC-Source"]);
        Assert.Equal("1", handler.Headers["X-TC-Contract"]);
        Assert.Equal("1793664000", handler.Headers["X-TC-Timestamp"]);
        Assert.Equal(PlatformSalesFeedSignature.Sign(Secret, "1793664000", body), handler.Headers["X-TC-Signature"]);
        Assert.StartsWith("application/json", handler.ContentType, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Ok_ReadsAcceptedDuplicatesAndRejected()
    {
        Guid accepted = Guid.NewGuid();
        Guid duplicate = Guid.NewGuid();
        Guid rejected = Guid.NewGuid();
        Handler handler = new(HttpStatusCode.OK,
            "{\"accepted\":[\"" + accepted + "\"],\"duplicates\":[\"" + duplicate + "\"],\"rejected\":[{\"id\":\"" + rejected + "\",\"error\":\"amount missing\"}]}");

        PlatformSalesFeedAnswer answer = await Client(handler).SendAsync("{}", CancellationToken.None);

        Assert.Equal(new[] { accepted }, answer.Accepted);
        Assert.Equal(new[] { duplicate }, answer.Duplicates);
        Assert.Equal(new PlatformSalesFeedRejection(rejected, "amount missing"), Assert.Single(answer.Rejected));
    }

    [Theory]
    [InlineData(HttpStatusCode.RequestEntityTooLarge, PlatformSalesFeedAnswerKind.TooLarge)]
    [InlineData(HttpStatusCode.BadRequest, PlatformSalesFeedAnswerKind.Refused)]
    [InlineData(HttpStatusCode.Unauthorized, PlatformSalesFeedAnswerKind.Refused)]
    [InlineData(HttpStatusCode.NotFound, PlatformSalesFeedAnswerKind.Retry)]
    [InlineData(HttpStatusCode.InternalServerError, PlatformSalesFeedAnswerKind.Retry)]
    [InlineData(HttpStatusCode.ServiceUnavailable, PlatformSalesFeedAnswerKind.Retry)]
    public async Task Answers_Map(HttpStatusCode status, PlatformSalesFeedAnswerKind expected)
    {
        PlatformSalesFeedAnswer answer = await Client(new Handler(status, "{}")).SendAsync("{}", CancellationToken.None);

        Assert.Equal(expected, answer.Kind);
        Assert.Equal("http_" + (int)status, answer.Code);
    }

    [Fact]
    public async Task NoAnswer_IsRetried_UnreadableOkToo()
    {
        Assert.Equal(PlatformSalesFeedAnswerKind.Retry, (await Client(new Handler(throws: true)).SendAsync("{}", CancellationToken.None)).Kind);
        Assert.Equal("no_answer", (await Client(new Handler(throws: true)).SendAsync("{}", CancellationToken.None)).Code);
        Assert.Equal(PlatformSalesFeedAnswerKind.Retry, (await Client(new Handler(HttpStatusCode.OK, "not json")).SendAsync("{}", CancellationToken.None)).Kind);
    }

    [Theory]
    [InlineData("", Secret, "vixly", "url_missing")]
    [InlineData(Url, "", "vixly", "secret_missing")]
    [InlineData(Url, Secret, "", "source_missing")]
    [InlineData("http://techcrafters.dyndns.ws/api/sales-sources/vixly/events", Secret, "vixly", "url_not_https")]
    [InlineData("techcrafters.ltd/api/sales-sources/vixly/events", Secret, "vixly", "url_not_https")]
    [InlineData("https://techcrafters.ltd", Secret, "vixly", "url_no_path")]
    [InlineData("https://techcrafters.ltd/", Secret, "vixly", "url_no_path")]
    public void Off_NamesTheSettingThatIsMissingOrInvalid(string url, string secret, string source, string problem)
    {
        PlatformSalesFeedClient client = Client(new Handler(HttpStatusCode.OK, "{}"), url, secret, source);

        Assert.False(client.Configured);
        Assert.Equal(problem, client.SettingsProblem);
    }

    [Theory]
    [InlineData(Url)]
    [InlineData(" " + Url + "/ ")]
    public void On_WithTheFullEventAddress(string url)
    {
        PlatformSalesFeedClient client = Client(new Handler(HttpStatusCode.OK, "{}"), url);

        Assert.True(client.Configured);
        Assert.Null(client.SettingsProblem);
    }

    private static PlatformSalesFeedClient Client(Handler handler, string url = Url, string secret = Secret, string source = "vixly")
    {
        return new PlatformSalesFeedClient(new HttpClient(handler), Options.Create(new PlatformSalesFeedOptions { Url = url, Secret = secret, Source = source }),
            new FixedTimeProvider(Now), NullLogger<PlatformSalesFeedClient>.Instance);
    }

    private sealed class Handler : HttpMessageHandler
    {
        private readonly HttpStatusCode _status;

        private readonly string _answer;

        private readonly bool _throws;

        public Handler(HttpStatusCode status = HttpStatusCode.OK, string answer = "{}", bool throws = false)
        {
            _status = status;
            _answer = answer;
            _throws = throws;
        }

        public string? Address { get; private set; }

        public string? Body { get; private set; }

        public string? ContentType { get; private set; }

        public Dictionary<string, string> Headers { get; } = new(StringComparer.OrdinalIgnoreCase);

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (_throws)
            {
                throw new HttpRequestException("down");
            }

            Address = request.RequestUri!.ToString();
            Body = await request.Content!.ReadAsStringAsync(cancellationToken);
            ContentType = request.Content.Headers.ContentType?.ToString();

            foreach (KeyValuePair<string, IEnumerable<string>> header in request.Headers)
            {
                Headers[header.Key] = string.Join(",", header.Value);
            }

            return new HttpResponseMessage(_status) { Content = new StringContent(_answer) };
        }
    }
}
