using Vixly.Operations.Domain.Common;
using Vixly.Shared.Kernel.Domain;

namespace Vixly.Operations.Domain.Items;

// =========================================================
// FILE: OperationsItemCatalogueEntry.cs
//
// PURPOSE:
// One line of the company's item catalogue: how an everyday item is described and coded for customs (owner, 2026-11-01).
//
// RESPONSIBILITIES:
// - Hold the description printed on documents, the words customers use for it, its customs code and unit
// - Count how often it was used
//
// IMPORTANT NOTES:
// - Each company keeps its own; a starter list is loaded once, then it is the company's to change
// - Plain text only; every page encodes output
// =========================================================
public sealed class OperationsItemCatalogueEntry : OperationsEntity
{
    public const int MatchesMaxLength = 400;

    public const int MaxPerCompany = 2000;

    private OperationsItemCatalogueEntry()
    {
        Description = string.Empty;
        HsCode = string.Empty;
    }

    private OperationsItemCatalogueEntry(Guid tenantId, DateTimeOffset createdUtc)
        : base(tenantId, createdUtc)
    {
        Description = string.Empty;
        HsCode = string.Empty;
    }

    public string Description { get; private set; }

    // Other words customers use for it, separated by commas ("sneakers, tennis, zapatillas").
    public string? Matches { get; private set; }

    public string HsCode { get; private set; }

    public OperationsItemUnit Unit { get; private set; }

    public int UseCount { get; private set; }

    public static OperationsItemCatalogueEntry Create(Guid tenantId, string? description, string? matches, string? hsCode, OperationsItemUnit unit, DateTimeOffset utcNow)
    {
        OperationsItemCatalogueEntry entry = new(tenantId, utcNow);
        entry.Change(description, matches, hsCode, unit, utcNow);
        return entry;
    }

    public void Change(string? description, string? matches, string? hsCode, OperationsItemUnit unit, DateTimeOffset utcNow)
    {
        string shown = OperationsParcelItem.Text(description, OperationsParcelItem.DescriptionMaxLength) ?? throw new DomainException(OperationsItemErrors.DescriptionInvalid);
        string? also = string.IsNullOrWhiteSpace(matches) ? null : matches.Trim();

        if (also is { Length: > MatchesMaxLength } || (also is not null && (also.Contains('<', StringComparison.Ordinal) || also.Contains('>', StringComparison.Ordinal))))
        {
            throw new DomainException(OperationsItemErrors.MatchesInvalid);
        }

        if (!Enum.IsDefined(unit))
        {
            throw new DomainException(OperationsItemErrors.UnitInvalid);
        }

        (Description, Matches, HsCode, Unit) = (shown, also, OperationsHsCode.Normalize(hsCode) ?? throw new DomainException(OperationsItemErrors.CodeInvalid), unit);
        MarkAsModified(utcNow);
    }

    public void Used(DateTimeOffset utcNow)
    {
        UseCount++;
        MarkAsModified(utcNow);
    }

    // The plain phrases it is found by: its description and each "also matches" phrase.
    public IEnumerable<string> Phrases() =>
        new[] { Description }.Concat((Matches ?? string.Empty).Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            .Select(OperationsItemWords.Plain).Where(phrase => phrase.Length > 1).Distinct(StringComparer.Ordinal);
}
