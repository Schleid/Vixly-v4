using System.Text.Json;
using Vixly.Operations.Application.Site;

namespace Vixly.Operations.Web.Site;

// =========================================================
// FILE: OperationsSiteJsonLd.cs
//
// PURPOSE:
// The schema.org description of a tenant for search engines (Slice 1B).
//
// RESPONSIBILITIES:
// - Describe the organization: name, address, logo, contact points per country, social profiles
// - Describe each public location with its address (no phone: customers call the call centre)
//
// IMPORTANT NOTES:
// - Everything is about the tenant, never Vixly
// - JSON escaping keeps "<" and "&" out of the script block
// =========================================================
public static class OperationsSiteJsonLd
{
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web);

    public static string Build(OperationsSitePage page, string description)
    {
        ArgumentNullException.ThrowIfNull(page);

        OperationsPublicSite site = page.Site;
        string url = page.Origin + "/";
        List<object> graph = new()
        {
            new Dictionary<string, object?>
            {
                ["@type"] = "Organization",
                ["@id"] = url + "#organization",
                ["name"] = page.Company,
                ["url"] = url,
                ["description"] = description,
                ["logo"] = page.Image(site.Settings.LogoImageId) is { } logo ? page.Origin + logo : null,
                ["sameAs"] = new[] { site.Settings.FacebookUrl, site.Settings.InstagramUrl, site.Settings.XUrl, site.Settings.LinkedInUrl, site.Settings.YouTubeUrl, site.Settings.TikTokUrl }
                    .Where(link => link is not null).ToArray(),
                ["contactPoint"] = site.Contacts.SelectMany(contact => contact.Phones.Select(phone => new Dictionary<string, object?>
                {
                    ["@type"] = "ContactPoint",
                    ["telephone"] = phone,
                    ["email"] = contact.Email,
                    ["contactType"] = "customer service",
                    ["areaServed"] = contact.CountryCode
                })).ToArray()
            }
        };

        graph.AddRange(site.Locations.Select(location => new Dictionary<string, object?>
        {
            ["@type"] = "LocalBusiness",
            ["@id"] = url + "#location-" + location.Id.ToString("N"),
            ["name"] = page.Company + " - " + location.Name,
            ["parentOrganization"] = new Dictionary<string, object?> { ["@id"] = url + "#organization" },
            ["address"] = new Dictionary<string, object?>
            {
                ["@type"] = "PostalAddress",
                ["streetAddress"] = location.Line2 is null ? location.Line1 : location.Line1 + ", " + location.Line2,
                ["addressLocality"] = location.City,
                ["addressRegion"] = location.Region,
                ["postalCode"] = location.PostalCode,
                ["addressCountry"] = location.CountryCode
            }
        }));

        return JsonSerializer.Serialize(new Dictionary<string, object?> { ["@context"] = "https://schema.org", ["@graph"] = graph }, Options);
    }
}
