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

    // Review item 3: the email belongs to a customer account; the message never says where.
    public static Error EmailUnavailable { get; } = Error.Create("ops.staff.email_unavailable");

    public static Error PasswordWrong { get; } = Error.Create("ops.staff.password_wrong");

    // Slice 7 step 4 (R-8): a partner agent is invited for a warehouse run by a partner, while the plan includes partner agents.
    public static Error PartnerWarehouseInvalid { get; } = Error.Create("ops.staff.partner_warehouse_invalid");

    public static Error PartnerAgentsOff { get; } = Error.Create("ops.staff.partner_agents_off");
}
