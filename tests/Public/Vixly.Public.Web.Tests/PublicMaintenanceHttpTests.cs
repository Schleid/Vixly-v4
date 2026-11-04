using System.Net;
using Microsoft.Extensions.DependencyInjection;
using Vixly.Shared.Hosting.Maintenance;

namespace Vixly.Public.Web.Tests;

// =========================================================
// FILE: PublicMaintenanceHttpTests.cs
//
// PURPOSE:
// Proves the maintenance window on a real site pipeline (Control backlog 2026-10-22, section 1).
//
// RESPONSIBILITIES:
// - An announced window shows the notice banner
// - A running window closes the site with 503, Retry-After and the message; health stays open
// - The pass link opens the site for the window; a wrong link opens nothing
// - Platform unreachable: the site stays open
//
// IMPORTANT NOTES:
// - The shared middleware is the same for the company websites, portals and staff workspaces
// =========================================================
public sealed class PublicMaintenanceHttpTests
{
    [Fact]
    public async Task Announced_ShowsTheBanner_Running_ClosesTheSite_AndThePassLinkOpensIt()
    {
        await using PublicSiteTestHost host = await PublicSiteTestHost.StartAsync();
        VixlyMaintenanceMonitor monitor = host.Services.GetRequiredService<VixlyMaintenanceMonitor>();
        DateTimeOffset now = DateTimeOffset.UtcNow;
        Guid id = Guid.NewGuid();

        host.Platform.Answer(HttpMethod.Get, "maintenance?site=public", HttpStatusCode.OK, Window(id, now.AddHours(3), now.AddHours(5), false));
        await monitor.RefreshAsync("public", CancellationToken.None);
        string page = await (await host.Client().GetAsync("/en/features")).Content.ReadAsStringAsync();
        Assert.Contains("vx-maintenance-banner", page, StringComparison.Ordinal);
        Assert.Contains("Database upgrade", page, StringComparison.Ordinal);

        host.Platform.Answer(HttpMethod.Get, "maintenance?site=public", HttpStatusCode.OK, Window(id, now.AddMinutes(-5), now.AddHours(1), true));
        await monitor.RefreshAsync("public", CancellationToken.None);
        HttpResponseMessage closed = await host.Client().GetAsync("/en/features");
        Assert.Equal(HttpStatusCode.ServiceUnavailable, closed.StatusCode);
        Assert.True(closed.Headers.RetryAfter is not null);
        Assert.Equal("1", closed.Headers.GetValues(VixlyMaintenance.MarkerHeader).Single());
        Assert.Contains("Database upgrade", await closed.Content.ReadAsStringAsync(), StringComparison.Ordinal);
        Assert.NotEqual(HttpStatusCode.ServiceUnavailable, (await host.Client().GetAsync("/health/live")).StatusCode);

        Assert.Equal(HttpStatusCode.NotFound, (await host.Client().GetAsync("/maintenance-pass/wrong")).StatusCode);

        host.Platform.Answer(HttpMethod.Post, "maintenance/pass", HttpStatusCode.OK, "{\"endsUtc\":\"" + now.AddHours(1).ToString("O") + "\"}");
        HttpResponseMessage pass = await host.Client().GetAsync("/maintenance-pass/it-team-link");
        Assert.Equal(HttpStatusCode.Redirect, pass.StatusCode);
        string cookie = pass.Headers.GetValues("Set-Cookie").Single(value => value.StartsWith(VixlyMaintenance.CookieName, StringComparison.Ordinal)).Split(';')[0];

        using HttpRequestMessage through = new(HttpMethod.Get, "/en/features");
        through.Headers.Add("Cookie", cookie);
        Assert.Equal(HttpStatusCode.OK, (await host.Client().SendAsync(through)).StatusCode);
    }

    // Owner, 2026-10-28: a window closing only staff workspaces is passed at /staff/maintenance-pass/{code}.
    [Theory]
    [InlineData("/maintenance-pass/abc", "abc", "/")]
    [InlineData("/maintenance-pass/abc/", "abc", "/")]
    [InlineData("/staff/maintenance-pass/abc", "abc", "/staff/")]
    [InlineData("/Staff/Maintenance-Pass/abc", "abc", "/Staff/")]
    [InlineData("/a/b/maintenance-pass/abc", null, null)]
    [InlineData("/\\evil.com/maintenance-pass/abc", null, null)]
    [InlineData("/staff/customers", null, null)]
    [InlineData("/", null, null)]
    public void PassLink_AtTheRootOrUnderTheSite_NeverARedirectElsewhere(string path, string? token, string? home)
    {
        (string Token, string Home)? link = VixlyMaintenance.PassLink(new Microsoft.AspNetCore.Http.PathString(path));

        Assert.Equal(token, link?.Token);
        Assert.Equal(home, link?.Home);
    }

    [Fact]
    public async Task PlatformUnreachable_LeavesTheSiteOpen()
    {
        await using PublicSiteTestHost host = await PublicSiteTestHost.StartAsync();
        await host.Services.GetRequiredService<VixlyMaintenanceMonitor>().RefreshAsync("public", CancellationToken.None);

        Assert.Equal(HttpStatusCode.OK, (await host.Client().GetAsync("/en/features")).StatusCode);
    }

    private static string Window(Guid id, DateTimeOffset starts, DateTimeOffset ends, bool running) =>
        "[{\"id\":\"" + id + "\",\"startsUtc\":\"" + starts.ToString("O") + "\",\"endsUtc\":\"" + ends.ToString("O") + "\",\"running\":" + (running ? "true" : "false") + ",\"messageEn\":\"Database upgrade\"}]";
}
