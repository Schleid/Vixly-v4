namespace Vixly.Operations.Application.UseCases.Holds.ReviewOperationsHoldAnswer;

// =========================================================
// FILE: ReviewOperationsHoldAnswerRequest.cs
//
// PURPOSE:
// Staff accept a customer's answer to a hold, which ends it, or decline it with what is still missing (Slice 3E).
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class ReviewOperationsHoldAnswerRequest
{
    public Guid AnswerId { get; init; }

    public bool Accept { get; init; }

    public string? Note { get; init; }

    public string CommandKey { get; init; } = string.Empty;

    public string PortalSlug { get; init; } = string.Empty;
}
