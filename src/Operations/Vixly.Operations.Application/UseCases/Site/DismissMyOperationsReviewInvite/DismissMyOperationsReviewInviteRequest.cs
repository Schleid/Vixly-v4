namespace Vixly.Operations.Application.UseCases.Site.DismissMyOperationsReviewInvite;

// =========================================================
// FILE: DismissMyOperationsReviewInviteRequest.cs
//
// PURPOSE:
// Input for DismissMyOperationsReviewInvite.
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - The opaque customer session; Operations finds the customer
// =========================================================
public sealed class DismissMyOperationsReviewInviteRequest
{
    public string SessionToken { get; init; } = string.Empty;
}
