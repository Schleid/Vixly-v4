using Vixly.Operations.Domain.Inspection;

namespace Vixly.Operations.Application.UseCases.Inspection.SaveOperationsInspectionRule;

// =========================================================
// FILE: SaveOperationsInspectionRuleRequest.cs
//
// PURPOSE:
// Adds an inspection rule, or takes one in or out of use (Slice 3C).
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class SaveOperationsInspectionRuleRequest
{
    public Guid? RuleId { get; init; }

    public OperationsInspectionRuleKind Kind { get; init; } = OperationsInspectionRuleKind.ListedItems;

    public string? Value { get; init; }

    public bool Active { get; init; } = true;
}
