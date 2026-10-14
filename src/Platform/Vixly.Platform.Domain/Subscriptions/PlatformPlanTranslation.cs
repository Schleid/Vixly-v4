using Vixly.Shared.Kernel.Domain;
using Vixly.Shared.Kernel.Localization;

namespace Vixly.Platform.Domain.Subscriptions;

// =========================================================
// FILE: PlatformPlanTranslation.cs
//
// PURPOSE:
// A plan's name and description in one language.
//
// RESPONSIBILITIES:
// - Hold the name and optional description for en, fr or es
//
// IMPORTANT NOTES:
// - Owned by PlatformPlan; one per language
// - English is required; French and Spanish fall back to English when missing
// =========================================================
public sealed class PlatformPlanTranslation
{
    private PlatformPlanTranslation()
    {
        Language = LanguageCode.Default;
        Name = string.Empty;
    }

    internal PlatformPlanTranslation(Guid platformPlanId, LanguageCode language, string? name, string? description)
    {
        PlatformPlanId = platformPlanId;
        Language = language;
        Name = string.Empty;
        Rename(name, description);
    }

    public Guid PlatformPlanId { get; private set; }

    public LanguageCode Language { get; private set; }

    public string Name { get; private set; }

    public string? Description { get; private set; }

    internal void Rename(string? name, string? description)
    {
        string trimmed = name?.Trim() ?? string.Empty;

        if (trimmed.Length == 0)
        {
            throw new DomainException(PlatformPlanErrors.NameRequired);
        }

        if (trimmed.Length > PlatformSubscriptionLimits.PlanNameMaxLength)
        {
            throw new DomainException(PlatformPlanErrors.NameTooLong);
        }

        Name = trimmed;
        Description = PlatformSubscriptionText.OptionalText(
            description,
            PlatformSubscriptionLimits.PlanDescriptionMaxLength,
            PlatformPlanErrors.DescriptionTooLong);
    }
}
