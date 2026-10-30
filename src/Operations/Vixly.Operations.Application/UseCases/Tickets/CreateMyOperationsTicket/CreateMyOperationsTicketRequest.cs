using Vixly.Operations.Domain.Tickets;

namespace Vixly.Operations.Application.UseCases.Tickets.CreateMyOperationsTicket;

// =========================================================
// FILE: CreateMyOperationsTicketRequest.cs
//
// PURPOSE:
// A customer opens a ticket about a package the carrier delivered that is not in its account (Slice 4D step 4, A-6).
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class CreateMyOperationsTicketRequest
{
    public string SessionToken { get; init; } = string.Empty;

    // The warehouse it was shipped to; may be left out when the customer has only one.
    public Guid? WarehouseId { get; init; }

    public OperationsTicketDetails Details { get; init; } = new(null, null, null, null, null, null);

    public string? Message { get; init; }

    // The carrier's proof of delivery (PDF, PNG or JPEG, up to 10 MB), optional.
    public byte[]? Proof { get; init; }
}
