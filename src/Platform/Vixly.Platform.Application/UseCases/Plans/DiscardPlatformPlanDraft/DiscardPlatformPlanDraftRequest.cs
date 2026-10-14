namespace Vixly.Platform.Application.UseCases.Plans.DiscardPlatformPlanDraft;

// =========================================================
// FILE: DiscardPlatformPlanDraftRequest.cs
//
// PURPOSE:
// Discards the draft of a plan that has a published version.
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class DiscardPlatformPlanDraftRequest
{
    public Guid PlatformPlanId { get; init; }
}
