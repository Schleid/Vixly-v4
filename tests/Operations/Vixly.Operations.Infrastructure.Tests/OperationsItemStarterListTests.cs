using Vixly.Operations.Domain.Items;
using Vixly.Operations.Infrastructure.Items;

namespace Vixly.Operations.Infrastructure.Tests;

// =========================================================
// FILE: OperationsItemStarterListTests.cs
//
// PURPOSE:
// Proves the starter item catalogue shipped with the application can be read and is usable (owner, 2026-11-01).
//
// RESPONSIBILITIES:
// - Over a hundred everyday items, each with a 6-digit customs code, a unit and a unique description
//
// IMPORTANT NOTES:
// - No database
// =========================================================
public sealed class OperationsItemStarterListTests
{
    [Fact]
    public void StarterList_HasEverydayItems_WithSixDigitCodes()
    {
        OperationsItemStarterList list = new();

        Assert.True(list.Items.Count >= 100);
        Assert.All(list.Items, item =>
        {
            Assert.Equal(item.HsCode, OperationsHsCode.Normalize(item.HsCode));
            Assert.Equal(OperationsHsCode.MinDigits, item.HsCode.Length);
            Assert.True(Enum.IsDefined(item.Unit));
            OperationsItemCatalogueEntry.Create(Guid.NewGuid(), item.Description, item.Matches, item.HsCode, item.Unit, DateTimeOffset.UtcNow);
        });
        Assert.Equal(list.Items.Count, list.Items.Select(item => item.Description).Distinct(StringComparer.OrdinalIgnoreCase).Count());
        Assert.Contains(list.Items, item => item.Description == "Mobile phone" && item.HsCode == "851713");
    }
}
