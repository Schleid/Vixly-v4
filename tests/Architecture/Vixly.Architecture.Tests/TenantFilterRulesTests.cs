namespace Vixly.Architecture.Tests;

// =========================================================
// FILE: TenantFilterRulesTests.cs
//
// PURPOSE:
// Keeps reads across companies to the ones reviewed (Operations
// Slice 0: cross-tenant work is named and reviewed per use).
//
// RESPONSIBILITIES:
// - Allow IgnoreQueryFilters only where a design note names it
//
// IMPORTANT NOTES:
// - Review item 3 (2026-10-21): a work email is never a customer
//   email at any company, and the other way round; each check
//   answers only yes or no
// =========================================================
public sealed class TenantFilterRulesTests
{
    private static readonly Dictionary<string, int> Reviewed = new(StringComparer.Ordinal)
    {
        ["OperationsUserRepository.cs"] = 1,
        ["OperationsCustomerRepository.cs"] = 1
    };

    [Fact]
    public void IgnoreQueryFilters_OnlyInTheReviewedReads()
    {
        Dictionary<string, int> found = RepositoryLocator.CSharpFiles()
            .Where(path => path.Contains(Path.DirectorySeparatorChar + "src" + Path.DirectorySeparatorChar, StringComparison.Ordinal))
            .Select(path => (Name: Path.GetFileName(path), Count: CountOf(File.ReadAllText(path), "IgnoreQueryFilters(")))
            .Where(file => file.Count > 0)
            .ToDictionary(file => file.Name, file => file.Count, StringComparer.Ordinal);

        Assert.Equal(Reviewed.OrderBy(pair => pair.Key, StringComparer.Ordinal), found.OrderBy(pair => pair.Key, StringComparer.Ordinal));
    }

    private static int CountOf(string text, string value)
    {
        int count = 0;

        for (int index = text.IndexOf(value, StringComparison.Ordinal); index >= 0; index = text.IndexOf(value, index + value.Length, StringComparison.Ordinal))
        {
            count++;
        }

        return count;
    }
}
