using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Inspection.SaveOperationsInspectionRule;

// =========================================================
// FILE: SaveOperationsInspectionRuleResponse.cs
//
// PURPOSE:
// Adds an inspection rule, or takes one in or out of use (Slice 3C).
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class SaveOperationsInspectionRuleResponse
{
    private SaveOperationsInspectionRuleResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public Guid RuleId { get; private init; }

    public static SaveOperationsInspectionRuleResponse Success(Guid ruleId)
    {
        return new SaveOperationsInspectionRuleResponse { Succeeded = true, RuleId = ruleId };
    }

    public static SaveOperationsInspectionRuleResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new SaveOperationsInspectionRuleResponse { Error = error };
    }
}
