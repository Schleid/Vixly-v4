using Vixly.Shared.Kernel.Domain;

namespace Vixly.Operations.Domain.Branding;

// =========================================================
// FILE: OperationsBrandSettings.cs
//
// PURPOSE:
// One version of a tenant brand: name, colours, images, links and sections (Slice 1B).
//
// RESPONSIBILITIES:
// - Validate colours against WCAG 2.2 AA (white text on primary 4.5:1, accent against white 3:1)
// - Accept https links only
//
// IMPORTANT NOTES:
// - Held twice by the brand: draft and published
// =========================================================
public sealed class OperationsBrandSettings
{
    public const int DisplayNameMaxLength = 150;

    public const int LinkMaxLength = 300;

    private OperationsBrandSettings()
    {
        DisplayName = string.Empty;
        PrimaryColour = OperationsSitePresets.Colours(OperationsSitePreset.Ocean).Primary;
        AccentColour = OperationsSitePresets.Colours(OperationsSitePreset.Ocean).Accent;
    }

    public string DisplayName { get; private set; }

    public OperationsSitePreset Preset { get; private set; }

    public string PrimaryColour { get; private set; }

    public string AccentColour { get; private set; }

    public Guid? LogoImageId { get; private set; }

    public Guid? LogoDarkImageId { get; private set; }

    public Guid? FaviconImageId { get; private set; }

    public Guid? HeroImageId { get; private set; }

    public string? FacebookUrl { get; private set; }

    public string? InstagramUrl { get; private set; }

    public string? XUrl { get; private set; }

    public string? LinkedInUrl { get; private set; }

    public string? YouTubeUrl { get; private set; }

    public string? TikTokUrl { get; private set; }

    public OperationsSiteServices Services { get; private set; } = OperationsSiteServices.All;

    public bool ShowSteps { get; private set; } = true;

    public bool ShowFaq { get; private set; }

    public static OperationsBrandSettings Initial(string displayName)
    {
        return new OperationsBrandSettings { DisplayName = Name(displayName) };
    }

    public OperationsBrandSettings With(
        string displayName,
        OperationsSitePreset preset,
        string primaryColour,
        string accentColour,
        OperationsSiteServices services,
        bool showSteps,
        bool showFaq,
        (string? Facebook, string? Instagram, string? X, string? LinkedIn, string? YouTube, string? TikTok) links)
    {
        string primary = OperationsColour.Normalize(primaryColour);
        string accent = OperationsColour.Normalize(accentColour);

        if (OperationsColour.Contrast(primary, OperationsColour.White) < 4.5)
        {
            throw new DomainException(OperationsBrandingErrors.PrimaryContrastTooLow);
        }

        if (OperationsColour.Contrast(accent, OperationsColour.White) < 3)
        {
            throw new DomainException(OperationsBrandingErrors.AccentContrastTooLow);
        }

        OperationsBrandSettings copy = Copy();
        copy.DisplayName = Name(displayName);
        copy.Preset = Enum.IsDefined(preset) ? preset : OperationsSitePreset.Ocean;
        copy.PrimaryColour = primary;
        copy.AccentColour = accent;
        copy.Services = services & OperationsSiteServices.All;
        copy.ShowSteps = showSteps;
        copy.ShowFaq = showFaq;
        copy.FacebookUrl = Link(links.Facebook);
        copy.InstagramUrl = Link(links.Instagram);
        copy.XUrl = Link(links.X);
        copy.LinkedInUrl = Link(links.LinkedIn);
        copy.YouTubeUrl = Link(links.YouTube);
        copy.TikTokUrl = Link(links.TikTok);
        return copy;
    }

    public OperationsBrandSettings WithImage(OperationsSiteImageKind kind, Guid imageId)
    {
        OperationsBrandSettings copy = Copy();

        switch (kind)
        {
            case OperationsSiteImageKind.Logo: copy.LogoImageId = imageId; break;
            case OperationsSiteImageKind.LogoDark: copy.LogoDarkImageId = imageId; break;
            case OperationsSiteImageKind.Favicon: copy.FaviconImageId = imageId; break;
            case OperationsSiteImageKind.Hero: copy.HeroImageId = imageId; break;
        }

        return copy;
    }

    public OperationsBrandSettings WithoutImage(OperationsSiteImageKind kind)
    {
        OperationsBrandSettings copy = Copy();

        switch (kind)
        {
            case OperationsSiteImageKind.Logo: copy.LogoImageId = null; break;
            case OperationsSiteImageKind.LogoDark: copy.LogoDarkImageId = null; break;
            case OperationsSiteImageKind.Favicon: copy.FaviconImageId = null; break;
            case OperationsSiteImageKind.Hero: copy.HeroImageId = null; break;
        }

        return copy;
    }

    // A plan without theme customisation shows the default colours; the name and logos stay (W-4).
    public OperationsBrandSettings WithDefaultTheme()
    {
        OperationsBrandSettings copy = Copy();
        copy.Preset = OperationsSitePreset.Ocean;
        (copy.PrimaryColour, copy.AccentColour) = OperationsSitePresets.Colours(OperationsSitePreset.Ocean);
        return copy;
    }

    // A plan without content customisation shows the default home page: no main image, every service, the default sections (W-4).
    public OperationsBrandSettings WithDefaultContent()
    {
        OperationsBrandSettings copy = Copy();
        copy.HeroImageId = null;
        copy.Services = OperationsSiteServices.All;
        copy.ShowSteps = true;
        copy.ShowFaq = false;
        return copy;
    }

    public bool Uses(Guid imageId) => LogoImageId == imageId || LogoDarkImageId == imageId || FaviconImageId == imageId || HeroImageId == imageId;

    public OperationsBrandSettings Copy() => (OperationsBrandSettings)MemberwiseClone();

    private static string Name(string? value)
    {
        string trimmed = value?.Trim() ?? string.Empty;

        if (trimmed.Length == 0)
        {
            throw new DomainException(OperationsBrandingErrors.DisplayNameRequired);
        }

        return trimmed.Length > DisplayNameMaxLength ? throw new DomainException(OperationsBrandingErrors.DisplayNameTooLong) : trimmed;
    }

    private static string? Link(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        string trimmed = value.Trim();
        return trimmed.Length <= LinkMaxLength
            && Uri.TryCreate(trimmed, UriKind.Absolute, out Uri? uri)
            && uri.Scheme == Uri.UriSchemeHttps
            && string.IsNullOrEmpty(uri.UserInfo)
                ? uri.AbsoluteUri
                : throw new DomainException(OperationsBrandingErrors.LinkInvalid);
    }
}
