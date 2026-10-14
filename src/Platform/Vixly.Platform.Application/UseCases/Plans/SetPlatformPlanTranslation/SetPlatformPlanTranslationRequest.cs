namespace Vixly.Platform.Application.UseCases.Plans.SetPlatformPlanTranslation;

// =========================================================
// FILE: SetPlatformPlanTranslationRequest.cs
//
// PURPOSE:
// Sets the name and description of a plan in one language.
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class SetPlatformPlanTranslationRequest
{
    public Guid PlatformPlanId { get; init; }

    public string Language { get; init; } = string.Empty;

    public string? Name { get; init; }

    public string? Description { get; init; }
}
