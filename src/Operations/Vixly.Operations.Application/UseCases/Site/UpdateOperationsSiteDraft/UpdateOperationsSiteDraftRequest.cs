using Vixly.Operations.Domain.Branding;

namespace Vixly.Operations.Application.UseCases.Site.UpdateOperationsSiteDraft;

// =========================================================
// FILE: UpdateOperationsSiteDraftRequest.cs
//
// PURPOSE:
// Saves the Owner changes to the draft site: settings and the texts of one language (Slice 1B).
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class UpdateOperationsSiteDraftRequest
{
    public string DisplayName { get; init; } = string.Empty;

    public OperationsSitePreset Preset { get; init; }

    public string PrimaryColour { get; init; } = string.Empty;

    public string AccentColour { get; init; } = string.Empty;

    public OperationsSiteServices Services { get; init; }

    public bool ShowSteps { get; init; }

    public bool ShowFaq { get; init; }

    public string? FacebookUrl { get; init; }

    public string? InstagramUrl { get; init; }

    public string? XUrl { get; init; }

    public string? LinkedInUrl { get; init; }

    public string? YouTubeUrl { get; init; }

    public string? TikTokUrl { get; init; }

    public string Language { get; init; } = "en";

    public IReadOnlyDictionary<string, string?> Texts { get; init; } = new Dictionary<string, string?>();
}
