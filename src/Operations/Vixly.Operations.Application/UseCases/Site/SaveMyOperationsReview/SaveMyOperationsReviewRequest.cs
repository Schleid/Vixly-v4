namespace Vixly.Operations.Application.UseCases.Site.SaveMyOperationsReview;

// =========================================================
// FILE: SaveMyOperationsReviewRequest.cs
//
// PURPOSE:
// Input for SaveMyOperationsReview.
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - The opaque customer session and the review as the customer wrote it
// =========================================================
public sealed class SaveMyOperationsReviewRequest
{
    public string SessionToken { get; init; } = string.Empty;

    public Vixly.Operations.Domain.Branding.OperationsReviewNameStyle NameStyle { get; init; }

    public string? Place { get; init; }

    public string? Quote { get; init; }

    public int? Rating { get; init; }

    public bool Agreed { get; init; }
}
