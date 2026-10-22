using Vixly.Operations.Domain.Common;
using Vixly.Operations.Domain.Inspection;
using Vixly.Shared.Kernel.Domain;

namespace Vixly.Operations.Domain.Charges;

// =========================================================
// FILE: OperationsItemType.cs
//
// PURPOSE:
// One of the company's item types: a barrel, a car, a TV (Slice 4B design section 7).
//
// RESPONSIBILITIES:
// - Keep the name and the words that point to it in a declaration
// - Say whether a declaration names it
//
// IMPORTANT NOTES:
// - One list per company; price lists price item types from it, never typed names
// - Never deleted, taken out of use
// =========================================================
public sealed class OperationsItemType : OperationsEntity
{
    private OperationsItemType()
    {
        Name = string.Empty;
    }

    private OperationsItemType(Guid tenantId, DateTimeOffset utcNow)
        : base(tenantId, utcNow)
    {
        Name = string.Empty;
    }

    public string Name { get; private set; }

    public string? Words { get; private set; }

    public bool IsActive { get; private set; }

    public static OperationsItemType Create(Guid tenantId, string? name, string? words, DateTimeOffset utcNow)
    {
        OperationsItemType type = new(tenantId, utcNow) { IsActive = true };
        type.Set(name, words);
        return type;
    }

    public void Change(string? name, string? words, bool active, DateTimeOffset utcNow)
    {
        Set(name, words);
        IsActive = active;
        MarkAsModified(utcNow);
    }

    public bool IsNamedIn(string? contents) =>
        Words is not null && contents is not null
        && OperationsDeclarationText.Phrases(Words).Any(phrase => OperationsDeclarationText.Contains(OperationsDeclarationText.Words(contents), phrase));

    private void Set(string? name, string? words)
    {
        Name = OperationsChargeLimits.Text(name, OperationsChargeLimits.NameMaxLength, OperationsChargeErrors.TextInvalid) ?? throw new DomainException(OperationsChargeErrors.NameRequired);
        Words = OperationsChargeLimits.Text(words, OperationsChargeLimits.WordsMaxLength, OperationsChargeErrors.TextInvalid);
    }
}
