using Vixly.Operations.Domain.Branding;
using Vixly.Operations.Domain.Locations;
using Vixly.Shared.Kernel.Domain;

namespace Vixly.Operations.Domain.Tests;

// =========================================================
// FILE: OperationsTenantSiteDomainTests.cs
//
// PURPOSE:
// Proves the tenant site rules of Slice 1B.
//
// RESPONSIBILITIES:
// - Colours keep WCAG AA contrast; every preset passes
// - Links are https; texts are plain and limited
// - Images are PNG, JPEG or WebP within size limits
// - The draft is invisible until published
// - Locations keep their own country
//
// IMPORTANT NOTES:
// - No database
// =========================================================
public sealed class OperationsTenantSiteDomainTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 18, 12, 0, 0, TimeSpan.Zero);

    private static readonly Guid Tenant = Guid.NewGuid();

    public static TheoryData<OperationsSitePreset> Presets()
    {
        TheoryData<OperationsSitePreset> data = new();
        foreach (OperationsSitePreset preset in Enum.GetValues<OperationsSitePreset>())
        {
            data.Add(preset);
        }

        return data;
    }

    [Theory]
    [MemberData(nameof(Presets))]
    public void EveryPreset_PassesTheContrastCheck(OperationsSitePreset preset)
    {
        (string primary, string accent) = OperationsSitePresets.Colours(preset);

        OperationsBrandSettings settings = Settings(primary, accent, preset);

        Assert.Equal(primary, settings.PrimaryColour);
        Assert.True(OperationsColour.Contrast(primary, OperationsColour.White) >= 4.5);
        Assert.True(OperationsColour.Contrast(accent, OperationsColour.White) >= 3);
    }

    [Fact]
    public void Contrast_FollowsWcag()
    {
        Assert.Equal(21, OperationsColour.Contrast("#000000", "#FFFFFF"), 1);
        Assert.Equal(1, OperationsColour.Contrast("#FFFFFF", "#FFFFFF"), 3);
        Assert.Equal("#1D4ED8", OperationsColour.Normalize(" #1d4ed8 "));
    }

    [Fact]
    public void LightColours_AreRefused_WithTheReason()
    {
        Assert.Equal("ops.brand.primary_contrast_too_low", Assert.Throws<DomainException>(() => Settings("#FFD60A", "#0E7490")).Error.Code);
        Assert.Equal("ops.brand.accent_contrast_too_low", Assert.Throws<DomainException>(() => Settings("#1D4ED8", "#FDE68A")).Error.Code);
        Assert.Equal("ops.brand.colour_invalid", Assert.Throws<DomainException>(() => Settings("blue", "#0E7490")).Error.Code);
    }

    [Theory]
    [InlineData("http://facebook.com/acme")]
    [InlineData("javascript:alert(1)")]
    [InlineData("https://")]
    public void Links_MustBeHttps(string link)
    {
        DomainException refused = Assert.Throws<DomainException>(() =>
            OperationsBrandSettings.Initial("Acme").With("Acme", OperationsSitePreset.Ocean, "#1D4ED8", "#0E7490", OperationsSiteServices.All, true, false, (link, null, null, null, null, null)));

        Assert.Equal("ops.brand.link_invalid", refused.Error.Code);
    }

    [Fact]
    public void Texts_ArePlain_Limited_AndKnown()
    {
        OperationsTenantBrand brand = OperationsTenantBrand.Create(Tenant, "Acme", Now);

        Assert.Equal("ops.brand.text_not_plain", Assert.Throws<DomainException>(() => brand.SetDraftText("en", "headline", "<script>alert(1)</script>", Now)).Error.Code);
        Assert.Equal("ops.brand.text_too_long", Assert.Throws<DomainException>(() => brand.SetDraftText("en", "headline", new string('a', 91), Now)).Error.Code);
        Assert.Equal("ops.brand.text_key_invalid", Assert.Throws<DomainException>(() => brand.SetDraftText("en", "custom_css", "x", Now)).Error.Code);
        Assert.Equal("ops.brand.language_invalid", Assert.Throws<DomainException>(() => brand.SetDraftText("de", "headline", "Hallo", Now)).Error.Code);
    }

    [Fact]
    public void Draft_IsInvisible_UntilPublished()
    {
        OperationsTenantBrand brand = OperationsTenantBrand.Create(Tenant, "Acme", Now);
        brand.ChangeDraft(brand.Draft.With("Acme Cargo", OperationsSitePreset.Forest, "#1E5631", "#A4580B", OperationsSiteServices.Air, false, true, (null, null, null, null, null, null)), Now.AddMinutes(1));
        brand.SetDraftText("fr", "headline", "Expédiez avec Acme", Now.AddMinutes(1));

        Assert.True(brand.HasUnpublishedChanges);
        Assert.Equal("Acme", brand.Published.DisplayName);
        Assert.Null(brand.Text(OperationsSiteTextStage.Published, "fr", "headline"));

        brand.Publish(Now.AddMinutes(2));

        Assert.False(brand.HasUnpublishedChanges);
        Assert.Equal("Acme Cargo", brand.DisplayName);
        Assert.Equal("#1E5631", brand.Published.PrimaryColour);
        Assert.Equal("Expédiez avec Acme", brand.Text(OperationsSiteTextStage.Published, "fr", "headline"));
        Assert.Equal(1, brand.PublishedVersion);

        brand.SetDraftText("fr", "headline", string.Empty, Now.AddMinutes(3));
        Assert.Null(brand.Text(OperationsSiteTextStage.Draft, "fr", "headline"));
        Assert.Equal("Expédiez avec Acme", brand.Text(OperationsSiteTextStage.Published, "fr", "headline"));
    }

    [Theory]
    [InlineData(OperationsSiteImageKind.Logo, "image/svg+xml", 1000, 400, 200, "ops.brand.image_type_not_allowed")]
    [InlineData(OperationsSiteImageKind.Logo, "image/png", OperationsSiteImage.MaxBytes + 1, 400, 200, "ops.brand.image_too_large")]
    [InlineData(OperationsSiteImageKind.Favicon, "image/png", OperationsSiteImage.MaxFaviconBytes + 1, 64, 64, "ops.brand.image_too_large")]
    [InlineData(OperationsSiteImageKind.Favicon, "image/png", 1000, 64, 32, "ops.brand.image_size_invalid")]
    [InlineData(OperationsSiteImageKind.Logo, "image/png", 1000, 8, 8, "ops.brand.image_size_invalid")]
    [InlineData(OperationsSiteImageKind.Hero, "image/jpeg", 1000, 5000, 3000, "ops.brand.image_size_invalid")]
    public void Images_OutsideTheRules_AreRefused(OperationsSiteImageKind kind, string type, int bytes, int width, int height, string code)
    {
        DomainException refused = Assert.Throws<DomainException>(() =>
            OperationsSiteImage.Create(Tenant, kind, type, new byte[bytes], width, height, new string('a', 64), Now));

        Assert.Equal(code, refused.Error.Code);
    }

    [Fact]
    public void Image_WithinTheRules_IsKept()
    {
        OperationsSiteImage image = OperationsSiteImage.Create(Tenant, OperationsSiteImageKind.Favicon, "image/webp", new byte[100], 64, 64, new string('b', 64), Now);

        Assert.Equal("image/webp", image.ContentType);
        Assert.Equal(100, image.Content.Length);
    }

    [Fact]
    public void Warehouse_KeepsTheCountryOfItsOwnAddress()
    {
        OperationsBranch haiti = OperationsBranch.Create(Tenant, Details("PAP", "Port-au-Prince", "HT"), OperationsBranchSide.Destination, Now);
        OperationsWarehouse miami = OperationsWarehouse.Create(Tenant, haiti.Id, Details("FLL", "Fort Lauderdale", "us"), OperationsWarehouseRoles.Receiving, true, Now);

        Assert.Equal("HT", haiti.Address.CountryCode);
        Assert.Equal("US", miami.Address.CountryCode);
        Assert.True(miami.IsPublic);
    }

    [Fact]
    public void Locations_RefuseBadValues()
    {
        Assert.Equal("ops.locations.country_invalid", Assert.Throws<DomainException>(() => Details("A", "B", "XX")).Error.Code);
        Assert.Equal("ops.locations.phone_not_valid", Assert.Throws<DomainException>(() =>
            OperationsCountryContact.Create(Tenant, "HT", null, new[] { "+50948605107", "call us" }, null, null, null, 0, Now)).Error.Code);
        Assert.Equal("ops.locations.phone_not_valid", Assert.Throws<DomainException>(() =>
            OperationsCountryContact.Create(Tenant, "HT", null, new[] { "48605107" }, null, null, null, 0, Now)).Error.Code);
        Assert.Equal("ops.locations.phones_required", Assert.Throws<DomainException>(() =>
            OperationsCountryContact.Create(Tenant, "HT", null, new[] { " ", "" }, null, null, null, 0, Now)).Error.Code);
        Assert.Equal("ops.locations.too_many_phones", Assert.Throws<DomainException>(() =>
            OperationsCountryContact.Create(Tenant, "HT", null, Enumerable.Range(0, 13).Select(index => "+5093214" + (9800 + index)).ToArray(), null, null, null, 0, Now)).Error.Code);
        Assert.Equal("ops.locations.role_required", Assert.Throws<DomainException>(() =>
            OperationsWarehouse.Create(Tenant, Guid.NewGuid(), Details("W1", "Main", "US"), OperationsWarehouseRoles.None, false, Now)).Error.Code);
    }

    [Fact]
    public void CountryContact_KeepsSeveralNumbers_InOrder_WithoutRepeats()
    {
        OperationsCountryContact contact = OperationsCountryContact.Create(
            Tenant, "ht", "Call centre", new[] { "+509 4860-5107", "+509-3994-6752", "", "+50948605107", "+509 3214 9800" }, null, null, null, 0, Now);

        Assert.Equal("HT", contact.CountryCode);
        Assert.Equal(new[] { "+50948605107", "+50939946752", "+50932149800" }, contact.Phones);
    }

    private static OperationsBrandSettings Settings(string primary, string accent, OperationsSitePreset preset = OperationsSitePreset.Ocean)
    {
        return OperationsBrandSettings.Initial("Acme").With("Acme", preset, primary, accent, OperationsSiteServices.All, true, false, (null, null, null, null, null, null));
    }

    private static OperationsLocationDetails Details(string code, string city, string country)
    {
        return new OperationsLocationDetails(code, city + " office", OperationsPostalAddress.Create("1 Main Street", null, city, null, null, country), "+50922223333", null, "Mon-Fri 8-17", true);
    }
}
