using Vixly.Operations.Domain.Common;
using Vixly.Shared.Kernel.Domain;

namespace Vixly.Operations.Domain.Branding;

// =========================================================
// FILE: OperationsTenantBrand.cs
//
// PURPOSE:
// How a tenant presents itself: the draft and the published site (ADR-0012 W-2, Slice 1B).
//
// RESPONSIBILITIES:
// - Hold the draft and published settings and texts
// - Change the draft; publish copies it to the public site
//
// IMPORTANT NOTES:
// - One per tenant
// - Visitors only ever see the published version; the Owner previews the draft
// =========================================================
public sealed class OperationsTenantBrand : OperationsEntity
{
    public const int DisplayNameMaxLength = OperationsBrandSettings.DisplayNameMaxLength;

    private readonly List<OperationsSiteText> _texts = new();

    private OperationsTenantBrand()
    {
        Draft = null!;
        Published = null!;
    }

    private OperationsTenantBrand(Guid tenantId, OperationsBrandSettings initial, DateTimeOffset createdUtc)
        : base(tenantId, createdUtc)
    {
        Draft = initial;
        Published = initial.Copy();
        PublishedUtc = createdUtc;
    }

    public OperationsBrandSettings Draft { get; private set; }

    public OperationsBrandSettings Published { get; private set; }

    public IReadOnlyList<OperationsSiteText> Texts => _texts;

    public int PublishedVersion { get; private set; }

    public DateTimeOffset PublishedUtc { get; private set; }

    public DateTimeOffset? DraftChangedUtc { get; private set; }

    public string DisplayName => Published.DisplayName;

    public bool HasUnpublishedChanges => DraftChangedUtc is not null && DraftChangedUtc > PublishedUtc;

    public static OperationsTenantBrand Create(Guid tenantId, string displayName, DateTimeOffset utcNow)
    {
        return new OperationsTenantBrand(tenantId, OperationsBrandSettings.Initial(displayName), utcNow);
    }

    public string? Text(OperationsSiteTextStage stage, string language, string key)
    {
        return _texts.FirstOrDefault(text => text.Stage == stage && text.Language == language && text.Key == key)?.Value;
    }

    public IReadOnlyDictionary<string, string> TextsFor(OperationsSiteTextStage stage, string language)
    {
        return _texts.Where(text => text.Stage == stage && text.Language == language).ToDictionary(text => text.Key, text => text.Value, StringComparer.Ordinal);
    }

    public void ChangeDraft(OperationsBrandSettings settings, DateTimeOffset utcNow)
    {
        Draft = settings ?? throw new ArgumentNullException(nameof(settings));
        Touch(utcNow);
    }

    // Empty clears the text, so the page shows its default again.
    public void SetDraftText(string language, string key, string? value, DateTimeOffset utcNow)
    {
        string trimmed = value?.Trim() ?? string.Empty;
        string checkedValue = OperationsSiteText.Check(language, key, trimmed);
        OperationsSiteText? existing = _texts.FirstOrDefault(text => text.Stage == OperationsSiteTextStage.Draft && text.Language == language && text.Key == key);

        if (checkedValue.Length == 0)
        {
            if (existing is not null)
            {
                _texts.Remove(existing);
            }
        }
        else if (existing is null)
        {
            _texts.Add(new OperationsSiteText(TenantId, Id, OperationsSiteTextStage.Draft, language, key, checkedValue, utcNow));
        }
        else if (existing.Value != checkedValue)
        {
            existing.Change(checkedValue, utcNow);
        }

        Touch(utcNow);
    }

    public void Publish(DateTimeOffset utcNow)
    {
        Published = Draft.Copy();
        _texts.RemoveAll(text => text.Stage == OperationsSiteTextStage.Published);
        _texts.AddRange(_texts
            .Where(text => text.Stage == OperationsSiteTextStage.Draft)
            .Select(text => new OperationsSiteText(TenantId, Id, OperationsSiteTextStage.Published, text.Language, text.Key, text.Value, utcNow))
            .ToList());
        PublishedVersion++;
        PublishedUtc = utcNow;
        MarkAsModified(utcNow);
    }

    // Kept for Slice 1A callers: renames both versions.
    public void Rename(string displayName, DateTimeOffset utcNow)
    {
        Draft = Draft.With(displayName, Draft.Preset, Draft.PrimaryColour, Draft.AccentColour, Draft.Services, Draft.ShowSteps, Draft.ShowFaq,
            (Draft.FacebookUrl, Draft.InstagramUrl, Draft.XUrl, Draft.LinkedInUrl, Draft.YouTubeUrl, Draft.TikTokUrl));
        Published = Published.With(displayName, Published.Preset, Published.PrimaryColour, Published.AccentColour, Published.Services, Published.ShowSteps, Published.ShowFaq,
            (Published.FacebookUrl, Published.InstagramUrl, Published.XUrl, Published.LinkedInUrl, Published.YouTubeUrl, Published.TikTokUrl));
        MarkAsModified(utcNow);
    }

    private void Touch(DateTimeOffset utcNow)
    {
        DraftChangedUtc = utcNow;
        MarkAsModified(utcNow);
    }
}
