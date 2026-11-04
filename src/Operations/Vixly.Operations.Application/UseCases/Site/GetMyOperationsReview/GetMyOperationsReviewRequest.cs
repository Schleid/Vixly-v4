namespace Vixly.Operations.Application.UseCases.Site.GetMyOperationsReview;

// =========================================================
// FILE: GetMyOperationsReviewRequest.cs
//
// PURPOSE:
// Input for GetMyOperationsReview.
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - The opaque customer session; Operations finds the customer
// =========================================================
public sealed class GetMyOperationsReviewRequest
{
    public string SessionToken { get; init; } = string.Empty;
}
