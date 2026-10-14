namespace Vixly.Platform.Application.UseCases.Plans.StartPlatformPlanDraft;

// =========================================================
// FILE: StartPlatformPlanDraftRequest.cs
//
// PURPOSE:
// Starts a new draft from the current version of a plan (D-S2).
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class StartPlatformPlanDraftRequest
{
    public Guid PlatformPlanId { get; init; }
}
