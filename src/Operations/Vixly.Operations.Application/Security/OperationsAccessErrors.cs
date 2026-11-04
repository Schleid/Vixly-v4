using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.Security;

// =========================================================
// FILE: OperationsAccessErrors.cs
//
// PURPOSE:
// Error codes for refused access (Slice 1B).
//
// RESPONSIBILITIES:
// - Declare each refusal as a code
//
// IMPORTANT NOTES:
// - Codes start ops.access.
// =========================================================
public static class OperationsAccessErrors
{
    public static Error SignInRequired { get; } = Error.Create("ops.access.sign_in_required");

    public static Error OwnerOnly { get; } = Error.Create("ops.access.owner_only");

    public static Error PermissionRequired { get; } = Error.Create("ops.access.permission_required");

    // A second factor confirmed within 5 minutes is needed (design section 3).
    public static Error StepUpRequired { get; } = Error.Create("ops.access.step_up_required");
}
