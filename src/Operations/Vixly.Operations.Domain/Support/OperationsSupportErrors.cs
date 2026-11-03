using Vixly.Operations.Domain.Access;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Domain.Support;

// =========================================================
// FILE: OperationsSupportErrors.cs
//
// PURPOSE:
// Error codes of the support desk (owner, 2026-10-29, design section 9).
//
// RESPONSIBILITIES:
// - Declare each refusal as a code with its arguments
//
// IMPORTANT NOTES:
// - Codes start ops.support. (L-1); texts live in OperationsText
// - Not found and not this caller's give the same answer to the agent
// =========================================================
public static class OperationsSupportErrors
{
    public static Error PhoneInvalid { get; } = Error.Create("ops.support.phone_invalid");

    public static Error NotFound { get; } = Error.Create("ops.support.not_found");

    // A carrier's tracking number, an amount or a change needs the caller's phone first (design 9.1).
    public static Error CallerRequired { get; } = Error.Create("ops.support.caller_required");

    public static Error LookupLimit { get; } = Error.Create("ops.support.lookup_limit", ("limit", OperationsAccessLimits.SupportLookupsPerHour));
}
