namespace Vixly.Operations.Application.UseCases.Site.ChangeOperationsReviewInvitations;

// =========================================================
// FILE: ChangeOperationsReviewInvitationsRequest.cs
//
// PURPOSE:
// Input for ChangeOperationsReviewInvitations.
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Carries the caller's input only
// =========================================================
public sealed class ChangeOperationsReviewInvitationsRequest
{
    public bool Ask { get; init; }
}
