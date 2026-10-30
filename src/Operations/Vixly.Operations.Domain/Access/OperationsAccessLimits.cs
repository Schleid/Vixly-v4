namespace Vixly.Operations.Domain.Access;

// =========================================================
// FILE: OperationsAccessLimits.cs
//
// PURPOSE:
// Limits of roles and staff actions (Slice 1D).
//
// RESPONSIBILITIES:
// - Hold the role name and reason lengths and the step-up window
//
// IMPORTANT NOTES:
// - Step-up: a second factor confirmed within 5 minutes (design section 3)
// =========================================================
public static class OperationsAccessLimits
{
    public const int RoleNameMaxLength = 60;

    public const int ReasonMaxLength = 300;

    public static readonly TimeSpan StepUpWindow = TimeSpan.FromMinutes(5);
}
