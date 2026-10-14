using Microsoft.AspNetCore.Builder;
using System.Net;
using System.Net.Http.Json;
using System.Text;
using Vixly.Contracts.Platform.Registration;

namespace Vixly.Platform.Api.Tests;

// =========================================================
// FILE: PlatformApiHttpTests.cs
//
// PURPOSE:
// Proves the HTTP guarantees of the public registration API.
//
// RESPONSIBILITIES:
// - Security headers, no caching, no unknown hosts
// - Body limit and per-IP rate limits (S-6)
// - Bot check fails closed; errors are codes plus localised text
// - Access-token endpoints need a bearer token
//
// IMPORTANT NOTES:
// - Runs the real host on a TestServer
// =========================================================
public sealed class PlatformApiHttpTests
{
    [Fact]
    public async Task Health_CarriesTheSecurityHeaders()
    {
        (WebApplication app, HttpClient client) = await PlatformApiTestHost.StartAsync();
        await using WebApplication _ = app;

        HttpResponseMessage response = await client.GetAsync("health/live");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("nosniff", response.Headers.GetValues("X-Content-Type-Options").Single());
        Assert.Equal("DENY", response.Headers.GetValues("X-Frame-Options").Single());
    }

    [Fact]
    public async Task UnknownHost_IsRefused()
    {
        (WebApplication app, HttpClient client) = await PlatformApiTestHost.StartAsync();
        await using WebApplication _ = app;
        client.BaseAddress = new Uri("https://evil.example/");

        HttpResponseMessage response = await client.GetAsync("health/live");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task SlugCheck_AnswersAReasonCode_InTheCallersLanguage_AndIsNotCached()
    {
        (WebApplication app, HttpClient client) = await PlatformApiTestHost.StartAsync();
        await using WebApplication _ = app;
        using HttpRequestMessage request = new(HttpMethod.Get, "registration/slugs/www");
        request.Headers.AcceptLanguage.ParseAdd("fr");

        HttpResponseMessage response = await client.SendAsync(request);
        PlatformSlugAvailability? answer = await response.Content.ReadFromJsonAsync<PlatformSlugAvailability>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.False(answer!.Available);
        Assert.Equal("platform.tenants.slug_reserved", answer.Reason!.Code);
        Assert.Contains("réservée", answer.Reason.Message, StringComparison.Ordinal);
        Assert.Equal("no-store", response.Headers.CacheControl!.ToString());
    }

    [Fact]
    public async Task Start_FailsClosed_WhenTheBotCheckHasNoSecret()
    {
        (WebApplication app, HttpClient client) = await PlatformApiTestHost.StartAsync();
        await using WebApplication _ = app;

        HttpResponseMessage response = await client.PostAsJsonAsync("registration", new StartPlatformRegistrationBody { BotToken = "anything" });
        PlatformApiError? error = await response.Content.ReadFromJsonAsync<PlatformApiError>();

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("platform.registrations.bot_check_failed", error!.Code);
    }

    [Fact]
    public async Task Start_IsLimitedToFivePerHourPerClient()
    {
        (WebApplication app, HttpClient client) = await PlatformApiTestHost.StartAsync();
        await using WebApplication _ = app;
        HttpStatusCode[] codes = new HttpStatusCode[6];

        for (int attempt = 0; attempt < codes.Length; attempt++)
        {
            codes[attempt] = (await client.PostAsJsonAsync("registration", new StartPlatformRegistrationBody())).StatusCode;
        }

        Assert.All(codes[..5], code => Assert.Equal(HttpStatusCode.BadRequest, code));
        Assert.Equal(HttpStatusCode.TooManyRequests, codes[5]);
    }

    [Fact]
    public async Task OversizedBody_IsRefused()
    {
        (WebApplication app, HttpClient client) = await PlatformApiTestHost.StartAsync();
        await using WebApplication _ = app;
        using StringContent body = new("{\"legalName\":\"" + new string('x', 20_000) + "\"}", Encoding.UTF8, "application/json");

        HttpResponseMessage response = await client.PostAsync("registration/verify", body);

        Assert.Equal(HttpStatusCode.RequestEntityTooLarge, response.StatusCode);
    }

    [Theory]
    [InlineData("GET", "registration/status")]
    [InlineData("POST", "registration/resend")]
    public async Task AccessEndpoints_NeedABearerToken(string method, string path)
    {
        (WebApplication app, HttpClient client) = await PlatformApiTestHost.StartAsync();
        await using WebApplication _ = app;

        HttpResponseMessage response = await client.SendAsync(new HttpRequestMessage(new HttpMethod(method), path));
        PlatformApiError? error = await response.Content.ReadFromJsonAsync<PlatformApiError>();

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal("platform.registrations.access_hash_invalid", error!.Code);
    }
}
