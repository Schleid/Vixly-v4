using Vixly.Operations.Application.Items;

namespace Vixly.Operations.Application.Tests;

// =========================================================
// FILE: OperationsItemDeclarationTests.cs
//
// PURPOSE:
// Proves how declared items make a package's contents and value (owner, 2026-11-01).
//
// RESPONSIBILITIES:
// - Blank lines left out; the summary and the total value
//
// IMPORTANT NOTES:
// - No database
// =========================================================
public sealed class OperationsItemDeclarationTests
{
    [Fact]
    public void Summary_ListsQuantityAndWords_AndAddsUpTheValue()
    {
        IReadOnlyList<OperationsItemDraft> lines = OperationsItemDeclaration.Filled(new[]
        {
            new OperationsItemDraft("Sneakers", 2m, 45m, false),
            new OperationsItemDraft("  ", 1m, 10m, false),
            new OperationsItemDraft("Phone case", 1m, 12.99m, true)
        });

        (string contents, decimal value) = OperationsItemDeclaration.Summary(lines);

        Assert.Equal(2, lines.Count);
        Assert.Equal("2 x Sneakers, 1 x Phone case", contents);
        Assert.Equal(102.99m, value);
        Assert.Empty(OperationsItemDeclaration.Filled(null));
    }

    [Fact]
    public void Summary_StaysWithinTheContentsLimit()
    {
        List<OperationsItemDraft> lines = Enumerable.Range(1, 30).Select(index => new OperationsItemDraft("A rather long description of item " + index, 1m, 1m, false)).ToList();

        (string contents, decimal _) = OperationsItemDeclaration.Summary(lines);

        Assert.Equal(Vixly.Operations.Domain.Receiving.OperationsReceivingLimits.ContentsMaxLength, contents.Length);
        Assert.EndsWith("\u2026", contents, StringComparison.Ordinal);
    }
}
