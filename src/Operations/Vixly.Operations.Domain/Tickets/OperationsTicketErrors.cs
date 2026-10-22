using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Domain.Tickets;

// =========================================================
// FILE: OperationsTicketErrors.cs
//
// PURPOSE:
// Error codes of tickets (Slice 4D step 4).
//
// RESPONSIBILITIES:
// - One code per refusal
//
// IMPORTANT NOTES:
// - Texts in OperationsText and CustomerText (L-1)
// =========================================================
public static class OperationsTicketErrors
{
    public static Error NotFound { get; } = Error.Create("ops.tickets.not_found");

    public static Error DetailsInvalid { get; } = Error.Create("ops.tickets.details_invalid");

    public static Error MessageInvalid { get; } = Error.Create("ops.tickets.message_invalid");

    public static Error ProofInvalid { get; } = Error.Create("ops.tickets.proof_invalid");

    public static Error TooManyMessages { get; } = Error.Create("ops.tickets.too_many_messages");

    public static Error ParcelNotTheirs { get; } = Error.Create("ops.tickets.parcel_not_theirs");

    public static Error AlreadyLinked { get; } = Error.Create("ops.tickets.already_linked");

    public static Error AnotherOpen { get; } = Error.Create("ops.tickets.another_open");
}
