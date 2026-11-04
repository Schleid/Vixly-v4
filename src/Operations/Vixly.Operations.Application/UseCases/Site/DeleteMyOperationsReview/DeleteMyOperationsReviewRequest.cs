namespace Vixly.Operations.Application.UseCases.Site.DeleteMyOperationsReview;

// =========================================================
// FILE: DeleteMyOperationsReviewRequest.cs
//
// PURPOSE:
// Input for DeleteMyOperationsReview.
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - The opaque customer session; Operations finds the customer
// =========================================================
public sealed class DeleteMyOperationsReviewRequest
{
    public string SessionToken { get; init; } = string.Empty;
}
