namespace Vixly.Contracts.Operations.Tickets;

// =========================================================
// FILE: CustomerTicketDto.cs
//
// PURPOSE:
// One of the customer's tickets (Slice 4D step 4).
//
// RESPONSIBILITIES:
// - Carry what the customer said, the status, the package staff linked and the messages
//
// IMPORTANT NOTES:
// - Messages are empty in the list; nothing in the warehouse shows before staff link it
// =========================================================
public sealed record CustomerTicketDto(Guid TicketId, string Status, string Carrier, string TrackingNumber, string? Store, string? OrderNumber, DateOnly? DeliveredOn, string Contents,
    bool HasProof, Guid? ParcelId, DateTimeOffset CreatedUtc, DateTimeOffset? ResolvedUtc, IReadOnlyList<CustomerTicketMessageDto> Messages);
