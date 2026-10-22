using Vixly.Customer.Web.Hosting;
using Vixly.Customer.Web.Tenancy;

namespace Vixly.Customer.Web.Tests;

// =========================================================
// FILE: CustomerHostSlugTests.cs
//
// PURPOSE:
// Proves which host names are tenant addresses (H-1).
//
// RESPONSIBILITIES:
// - Read the slug from a tenant host
// - Refuse other hosts
//
// IMPORTANT NOTES:
// - No host
// =========================================================
public sealed class CustomerHostSlugTests
{
    private static readonly CustomerHostingOptions Options = new()
    {
        BaseDomains = new[] { "techcrafters.ltd" },
        SlugSuffix = "-test",
        DevelopmentSlug = "northwind"
    };

    [Theory]
    [InlineData("northwind.techcrafters.ltd", "northwind")]
    [InlineData("NorthWind.TechCrafters.LTD.", "northwind")]
    [InlineData("northwind-test.techcrafters.ltd", "northwind")]
    [InlineData("localhost", "northwind")]
    [InlineData("techcrafters.ltd", null)]
    [InlineData("a.b.techcrafters.ltd", null)]
    [InlineData("northwind.example.com", null)]
    [InlineData("", null)]
    public void Slug_ComesFromTheHost(string host, string? expected)
    {
        Assert.Equal(expected, CustomerTenantMiddleware.Slug(host, Options));
    }
}
