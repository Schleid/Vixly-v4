namespace Vixly.Operations.Application.UseCases.Site.OpenOperationsReviewTicket;

// =========================================================
// FILE: OpenOperationsReviewTicketRequest.cs
//
// PURPOSE:
// Input for OpenOperationsReviewTicket.
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Carries the caller's input only
// =========================================================
public sealed class OpenOperationsReviewTicketRequest
{
    public Guid TestimonialId { get; init; }
}
