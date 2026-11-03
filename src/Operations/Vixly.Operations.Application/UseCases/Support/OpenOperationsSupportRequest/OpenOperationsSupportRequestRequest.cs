namespace Vixly.Operations.Application.UseCases.Support.OpenOperationsSupportRequest;

// =========================================================
// FILE: OpenOperationsSupportRequestRequest.cs
//
// PURPOSE:
// A support agent opens a call ticket for staff: what the caller asks about their shipment or package (owner, 2026-10-29, S-3).
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class OpenOperationsSupportRequestRequest
{
    public Guid CallerId { get; init; }

    public string? Number { get; init; }

    public string? Text { get; init; }

    // Owner, 2026-10-29 (ticket desk): the department that handles it.
    public Vixly.Operations.Domain.Tickets.OperationsTicketTopic Topic { get; init; } = Vixly.Operations.Domain.Tickets.OperationsTicketTopic.Other;
}
