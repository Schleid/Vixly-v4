using System.Net;

namespace Vixly.Operations.Web.Tests;

// =========================================================
// FILE: OperationsHelpHttpTests.cs
// PURPOSE:
// Verifies help cannot bypass the real tenant and sign-in gates.
// RESPONSIBILITIES:
// - Exercise anonymous requests before any database access.
// IMPORTANT NOTES:
// - Signed-in device and role walkthroughs remain separate checks.
// =========================================================
public sealed class OperationsHelpHttpTests
{
    [Theory]
    [InlineData("/staff/help")]
    [InlineData("/staff/help/your-account")]
    public async Task HelpRequiresSignInOnAKnownTenant(string path)
    {
        await using OperationsWebTestHost host = await OperationsWebTestHost.StartAsync();
        host.Platform.Tenant("helpco", Guid.NewGuid(), "active");
        using HttpClient client = host.Client("helpco.techcrafters.ltd");
        using HttpResponseMessage response = await client.GetAsync(path);
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal("/staff", response.Headers.Location?.OriginalString);
    }

    [Fact]
    public async Task UnknownTenantCannotReadHelp()
    {
        await using OperationsWebTestHost host = await OperationsWebTestHost.StartAsync();
        using HttpClient client = host.Client("unknown.techcrafters.ltd");
        using HttpResponseMessage response = await client.GetAsync("/staff/help");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
