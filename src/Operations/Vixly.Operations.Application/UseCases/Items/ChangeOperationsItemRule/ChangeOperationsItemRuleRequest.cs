using Vixly.Operations.Application.Items;
using Vixly.Operations.Domain.Items;
namespace Vixly.Operations.Application.UseCases.Items.ChangeOperationsItemRule;

// =========================================================
// FILE: ChangeOperationsItemRuleRequest.cs
//
// PURPOSE:
// Switches on or off the rule that items need a customs code and a country of origin before bundling (owner, 2026-11-01).
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class ChangeOperationsItemRuleRequest
{
    public bool Required { get; init; }
}
