using Vixly.Operations.Application.Items;
using Vixly.Operations.Domain.Items;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Items.ChangeOperationsItemRule;

// =========================================================
// FILE: ChangeOperationsItemRuleResponse.cs
//
// PURPOSE:
// Switches on or off the rule that items need a customs code and a country of origin before bundling (owner, 2026-11-01).
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class ChangeOperationsItemRuleResponse
{
    private ChangeOperationsItemRuleResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public static ChangeOperationsItemRuleResponse Success()
    {
        return new ChangeOperationsItemRuleResponse { Succeeded = true };
    }

    public static ChangeOperationsItemRuleResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new ChangeOperationsItemRuleResponse { Error = error };
    }
}
