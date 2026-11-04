using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Builder;
using Vixly.Contracts.Platform.Common;
using Vixly.Contracts.Platform.Leads;

namespace Vixly.Platform.Api.Tests;

// =========================================================
// FILE: PlatformPublicCallerHttpTests.cs
//
// PURPOSE:
// Proves that limits on the public routes follow the visitor, not the public site’s server (Stage 2).
//
// RESPONSIBILITIES:
// - A signed public caller token moves the limit to the visitor address it carries
// - A token without the public scope, or a forged one, changes nothing
// - The contact form is limited to 5 posts an hour per visitor and needs a passed bot check
//
// IMPORTANT NOTES:
// - No database: the bot check refuses first (empty secret), so the posts stop before storage
// =========================================================
public sealed class PlatformPublicCallerHttpTests : IDisposable
{
    private readonly PlatformServiceTokenSigner _public = new("public", "p1");

    private readonly PlatformServiceTokenSigner _operations = new("operations", "o1");

    private readonly PlatformServiceTokenSigner _forger = new("public", "p1");

    public void Dispose()
    {
        _public.Dispose();
        _operations.Dispose();
        _forger.Dispose();
    }

    private Task<(WebApplication App, HttpClient Client)> StartAsync()
    {
        return PlatformApiTestHost.StartAsync(new Dictionary<string, string?>
        {
            ["PlatformInternal:Hosts:0"] = "platform-internal.test",
            ["PlatformInternal:Callers:public:Scopes:0"] = "public",
            ["PlatformInternal:Callers:public:Keys:p1"] = _public.PublicKey,
            ["PlatformInternal:Callers:operations:Scopes:0"] = "tenant",
            ["PlatformInternal:Callers:operations:Keys:o1"] = _operations.PublicKey
        });
    }

    private static HttpRequestMessage Lead(string? caller)
    {
        HttpRequestMessage request = new(HttpMethod.Post, "leads")
        {
            Content = JsonContent.Create(new PlatformSalesLeadBody
            {
                CompanyName = "Rivera Freight",
                ContactName = "Ana Rivera",
                Email = "ana@rivera.example",
                Kind = "freight",
                Language = "en",
                SourcePage = "contact",
                BotToken = "token"
            })
        };

        if (caller is not null)
        {
            request.Headers.TryAddWithoutValidation("X-Vixly-Caller", caller);
        }

        return request;
    }

    private string Visitor(PlatformServiceTokenSigner signer, string ip)
    {
        return signer.Sign(new Dictionary<string, object?> { ["ip"] = ip });
    }

    [Fact]
    public async Task Leads_WithoutAPassedBotCheck_AreRefused()
    {
        (WebApplication app, HttpClient client) = await StartAsync();
        await using (app)
        {
            HttpResponseMessage response = await client.SendAsync(Lead(Visitor(_public, "198.51.100.1")));

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.Equal("platform.leads.bot_check_failed", (await response.Content.ReadFromJsonAsync<PlatformApiError>())!.Code);
        }
    }

    [Fact]
    public async Task Leads_AreLimitedPerVisitor_WhenThePublicSiteVouchesForTheAddress()
    {
        (WebApplication app, HttpClient client) = await StartAsync();
        await using (app)
        {
            List<HttpStatusCode> first = new();

            for (int attempt = 0; attempt < 6; attempt++)
            {
                first.Add((await client.SendAsync(Lead(Visitor(_public, "198.51.100.1")))).StatusCode);
            }

            HttpStatusCode second = (await client.SendAsync(Lead(Visitor(_public, "198.51.100.2")))).StatusCode;

            Assert.DoesNotContain(HttpStatusCode.TooManyRequests, first.Take(5));
            Assert.Equal(HttpStatusCode.TooManyRequests, first[5]);
            Assert.NotEqual(HttpStatusCode.TooManyRequests, second);
        }
    }

    [Fact]
    public async Task AWrongScopeOrAForgedToken_DoesNotMoveTheLimit()
    {
        (WebApplication app, HttpClient client) = await StartAsync();
        await using (app)
        {
            string[] callers =
            {
                Visitor(_operations, "198.51.100.10"),
                Visitor(_forger, "198.51.100.11"),
                Visitor(_operations, "198.51.100.12"),
                Visitor(_forger, "198.51.100.13"),
                "not-a-token"
            };
            List<HttpStatusCode> codes = new();

            foreach (string caller in callers)
            {
                codes.Add((await client.SendAsync(Lead(caller))).StatusCode);
            }

            // All five counted against the one connection address: the sixth is refused.
            codes.Add((await client.SendAsync(Lead(null))).StatusCode);

            Assert.DoesNotContain(HttpStatusCode.TooManyRequests, codes.Take(5));
            Assert.Equal(HttpStatusCode.TooManyRequests, codes[5]);
        }
    }
}
