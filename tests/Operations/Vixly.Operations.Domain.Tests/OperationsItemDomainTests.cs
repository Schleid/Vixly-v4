using Vixly.Operations.Domain.Items;
using Vixly.Operations.Domain.Settings;
using Vixly.Shared.Kernel.Domain;

namespace Vixly.Operations.Domain.Tests;

// =========================================================
// FILE: OperationsItemDomainTests.cs
//
// PURPOSE:
// Proves the rules of items in parcels and of the item catalogue (owner, 2026-11-01).
//
// RESPONSIBILITIES:
// - The customer's declaration, the customs code format, the classification, the catalogue match, the rule before bundling
//
// IMPORTANT NOTES:
// - No database
// =========================================================
public sealed class OperationsItemDomainTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 1, 12, 0, 0, TimeSpan.Zero);

    private static readonly Guid Tenant = Guid.NewGuid();

    [Fact]
    public void Declaration_KeepsTheCustomersWords_AndRefusesWhatCannotBeTrue()
    {
        OperationsParcelItem item = OperationsParcelItem.Declare(Tenant, "  Nike   sneakers ", 2m, 45.004m, "USD", false, 1, Now);

        Assert.Equal("Nike sneakers", item.Words);
        Assert.Equal(45.00m, item.ValueEach);
        Assert.Equal(90.00m, item.Total);
        Assert.False(item.IsComplete);
        Assert.Equal("ops.items.words_required", Assert.Throws<DomainException>(() => OperationsParcelItem.Declare(Tenant, " ", 1m, 1m, "USD", false, 1, Now)).Error.Code);
        Assert.Equal("ops.items.words_required", Assert.Throws<DomainException>(() => OperationsParcelItem.Declare(Tenant, "<b>x</b>", 1m, 1m, "USD", false, 1, Now)).Error.Code);
        Assert.Equal("ops.items.quantity_invalid", Assert.Throws<DomainException>(() => OperationsParcelItem.Declare(Tenant, "Shoes", 0m, 1m, "USD", false, 1, Now)).Error.Code);
        Assert.Equal("ops.items.value_invalid", Assert.Throws<DomainException>(() => OperationsParcelItem.Declare(Tenant, "Shoes", 1m, -1m, "USD", false, 1, Now)).Error.Code);
        Assert.Equal("ops.items.too_many", Assert.Throws<DomainException>(() => OperationsParcelItem.Declare(Tenant, "Shoes", 1m, 1m, "USD", false, OperationsParcelItem.MaxPerParcel + 1, Now)).Error.Code);
    }

    [Theory]
    [InlineData("6404.11", "640411", "6404.11")]
    [InlineData(" 6404 11 0000 ", "6404110000", "6404.11.0000")]
    [InlineData("640419", "640419", "6404.19")]
    public void CustomsCode_IsKeptAsDigits_AndShownWithDots(string typed, string stored, string shown)
    {
        Assert.Equal(stored, OperationsHsCode.Normalize(typed));
        Assert.Equal(shown, OperationsHsCode.Format(stored));
    }

    [Theory]
    [InlineData("12345")]
    [InlineData("12345678901")]
    [InlineData("64A4.11")]
    public void CustomsCode_RefusesFewerThan6_MoreThan10_OrLetters(string typed) =>
        Assert.Equal("ops.items.code_invalid", Assert.Throws<DomainException>(() => OperationsHsCode.Normalize(typed)).Error.Code);

    [Fact]
    public void Classification_IsCompleteWithCodeAndOrigin_AndNeverChangesTheDeclaration()
    {
        OperationsParcelItem item = OperationsParcelItem.Declare(Tenant, "phone case", 1m, 12.99m, "USD", true, 1, Now);
        Guid clerk = Guid.NewGuid();

        item.Classify("Phone case, plastic", "3926.90", "cn", OperationsItemUnit.Pieces, null, clerk, Now);

        Assert.True(item.IsComplete);
        Assert.Equal("392690", item.HsCode);
        Assert.Equal("CN", item.OriginCountryCode);
        Assert.Equal("phone case", item.Words);
        Assert.Equal(clerk, item.ClassifiedByUserId);
        Assert.Equal("ops.items.origin_invalid", Assert.Throws<DomainException>(() => item.Classify(null, "392690", "XX", OperationsItemUnit.Pieces, null, clerk, Now)).Error.Code);

        item.Classify(null, null, "CN", OperationsItemUnit.Pieces, null, clerk, Now);
        Assert.False(item.IsComplete);
    }

    [Fact]
    public void Catalogue_LongestPhraseWins_AccentsAndCaseDoNotMatter_AndOriginIsNeverGuessed()
    {
        OperationsItemCatalogueEntry phone = OperationsItemCatalogueEntry.Create(Tenant, "Mobile phone", "cell phone, telefono, téléphone", "851713", OperationsItemUnit.Pieces, Now);
        OperationsItemCatalogueEntry phoneCase = OperationsItemCatalogueEntry.Create(Tenant, "Phone case, plastic", "phone case, funda", "3926.90", OperationsItemUnit.Pieces, Now);
        OperationsItemCatalogueEntry shoes = OperationsItemCatalogueEntry.Create(Tenant, "Shoes, textile upper", "shoes, zapatos", "640419", OperationsItemUnit.Pairs, Now);
        OperationsItemCatalogueEntry[] catalogue = { phone, phoneCase, shoes };

        Assert.Same(phoneCase, OperationsItemMatcher.Find(catalogue, "Black PHONE CASE for iPhone"));
        Assert.Same(phone, OperationsItemMatcher.Find(catalogue, "Un TÉLÉPHONE Samsung"));
        Assert.Same(shoes, OperationsItemMatcher.Find(catalogue, "zapatos de niña"));
        Assert.Null(OperationsItemMatcher.Find(catalogue, "headphones"));
        Assert.Null(OperationsItemMatcher.Find(catalogue, "shoeshine kit"));

        OperationsParcelItem item = OperationsParcelItem.Declare(Tenant, "zapatos", 1m, 30m, "USD", false, 1, Now);
        item.Suggest(shoes, Now);
        Assert.Equal("640419", item.HsCode);
        Assert.Equal(OperationsItemUnit.Pairs, item.Unit);
        Assert.Null(item.OriginCountryCode);
        Assert.False(item.IsComplete);
    }

    [Fact]
    public void CatalogueEntry_NeedsADescriptionAndAValidCode()
    {
        Assert.Equal("ops.items.description_invalid", Assert.Throws<DomainException>(() => OperationsItemCatalogueEntry.Create(Tenant, " ", null, "640419", OperationsItemUnit.Pairs, Now)).Error.Code);
        Assert.Equal("ops.items.code_invalid", Assert.Throws<DomainException>(() => OperationsItemCatalogueEntry.Create(Tenant, "Shoes", null, null, OperationsItemUnit.Pairs, Now)).Error.Code);
        Assert.Equal("ops.items.unit_invalid", Assert.Throws<DomainException>(() => OperationsItemCatalogueEntry.Create(Tenant, "Shoes", null, "640419", 0, Now)).Error.Code);
    }

    [Fact]
    public void Rule_StartsWhenSwitchedOn_AndKeepsItsDateUntilSwitchedOff()
    {
        OperationsTenantSettings settings = OperationsTenantSettings.CreateDefault(Tenant, Now);
        Assert.Null(settings.ItemsRequiredSinceUtc);

        settings.RequireItems(true, Now);
        settings.RequireItems(true, Now.AddDays(1));
        Assert.Equal(Now, settings.ItemsRequiredSinceUtc);

        settings.RequireItems(false, Now.AddDays(2));
        Assert.Null(settings.ItemsRequiredSinceUtc);
    }
}
