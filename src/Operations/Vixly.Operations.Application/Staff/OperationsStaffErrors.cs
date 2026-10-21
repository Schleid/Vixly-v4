using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.Staff;

// =========================================================
// FILE: OperationsStaffErrors.cs
//
// PURPOSE:
// Error codes of the staff screens (Slice 1D).
//
// RESPONSIBILITIES:
// - Name the refusals the staff use cases add
//
// IMPORTANT NOTES:
// - Codes only; texts in OperationsText
// =========================================================
public static class OperationsStaffErrors
{
    public static Error EmailTaken { get; } = Error.Create("ops.staff.email_taken");

    public static Error PasswordWrong { get; } = Error.Create("ops.staff.password_wrong");
}
