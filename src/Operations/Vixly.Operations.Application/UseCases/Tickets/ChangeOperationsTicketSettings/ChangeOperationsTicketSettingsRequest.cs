namespace Vixly.Operations.Application.UseCases.Tickets.ChangeOperationsTicketSettings;

// =========================================================
// FILE: ChangeOperationsTicketSettingsRequest.cs
//
// PURPOSE:
// How long a ticket may wait before the managers are told: not taken after so many hours, not resolved after so many days (owner, 2026-10-29, ticket desk section 5).
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class ChangeOperationsTicketSettingsRequest
{
    public int TakeHours { get; init; }

    public int ResolveDays { get; init; }
}
