using Microsoft.Extensions.Options;
using Vixly.Operations.Infrastructure.Platform;

namespace Vixly.Operations.Infrastructure.Tests;

// =========================================================
// FILE: OperationsPortalLinksTests.cs
//
// PURPOSE:
// Proves that emailed links stay on the tenant's own host (Slice 2 review).
//
// RESPONSIBILITIES:
// - A slug is one host label; anything else is refused
//
// IMPORTANT NOTES:
// - No database
// =========================================================
public sealed class OperationsPortalLinksTests
{
    private static readonly OperationsPortalLinks Links = new(Options.Create(new OperationsLinkOptions { PortalUrlTemplate = "https://{slug}.techcrafters.ltd" }));

    [Fact]
    public void Slug_BuildsTheTenantLink()
    {
        Assert.Equal("https://andes-courier.techcrafters.ltd/customer/sign-in", Links.Page(" Andes-Courier ", "/customer/sign-in"));
    }

    [Theory]
    [InlineData("evil.com/")]
    [InlineData("evil.com")]
    [InlineData("a@b")]
    [InlineData("-andes")]
    [InlineData("andes-")]
    public void AnythingButAHostLabel_IsRefused(string slug)
    {
        Assert.Throws<ArgumentException>(() => Links.Page(slug, "/customer/sign-in"));
    }
}
