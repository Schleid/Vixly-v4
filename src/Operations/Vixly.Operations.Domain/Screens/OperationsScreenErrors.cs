using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Domain.Screens;

// =========================================================
// FILE: OperationsScreenErrors.cs
//
// PURPOSE:
// Error codes of customer screens (Slice 5F step 3).
//
// RESPONSIBILITIES:
// - Name each refusal
//
// IMPORTANT NOTES:
// - Codes only; texts in OperationsText (en, fr, es)
// =========================================================
public static class OperationsScreenErrors
{
    public static Error NameInvalid { get; } = Error.Create("ops.screens.name_invalid");

    public static Error NotFound { get; } = Error.Create("ops.screens.not_found");

    public static Error StepInvalid { get; } = Error.Create("ops.screens.step_invalid");

    public static Error NotPaired { get; } = Error.Create("ops.screens.not_paired");

    public static Error CodeInvalid { get; } = Error.Create("ops.screens.code_invalid");

    public static Error SignatureRequired { get; } = Error.Create("ops.screens.signature_required");

    public static Error AcceptRequired { get; } = Error.Create("ops.screens.accept_required");

    public static Error NotOffered { get; } = Error.Create("ops.screens.not_offered");
}
