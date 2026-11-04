using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Domain.Identity;

// =========================================================
// FILE: OperationsOutsideErrors.cs
//
// PURPOSE:
// Error codes of outside accounts (owner, 2026-10-29, S-5: outsourced call centres).
//
// RESPONSIBILITIES:
// - Declare each refusal as a code with its arguments
//
// IMPORTANT NOTES:
// - Codes start ops.staff.outside. (L-1); texts live in OperationsText
// =========================================================
public static class OperationsOutsideErrors
{
    // The Owner, an Administrator and a partner's agent are never outside accounts.
    public static Error NotForThisAccount { get; } = Error.Create("ops.staff.outside.not_for_this_account");

    public static Error DaysRequired { get; } = Error.Create("ops.staff.outside.days_required");

    public static Error HoursInvalid { get; } = Error.Create("ops.staff.outside.hours_invalid");

    public static Error EndsInThePast { get; } = Error.Create("ops.staff.outside.ends_in_the_past");
}
