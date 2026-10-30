using Vixly.Operations.Domain.Tickets;

namespace Vixly.Operations.Application.UseCases.Tickets.GetOperationsTicketProof;

// =========================================================
// FILE: GetOperationsTicketProofRequest.cs
//
// PURPOSE:
// The carrier's proof of delivery attached to a ticket, for staff (Slice 4D step 4).
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class GetOperationsTicketProofRequest
{
    public Guid TicketId { get; init; }
}
