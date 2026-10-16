using Vixly.Shared.Kernel.Domain;

namespace Vixly.Platform.Domain.Legal;

// =========================================================
// FILE: PlatformLegalDocument.cs
//
// PURPOSE:
// One version of one Vixly legal document (Slice 8).
//
// RESPONSIBILITIES:
// - Draft: label, acceptance rule, effective date and included versions can change
// - Publish: needs a file per language; dates fixed; never changed after
// - Supersede when the next version of the kind is published
//
// IMPORTANT NOTES:
// - Files live in PlatformLegalDocumentFile, so loading a document never loads PDFs
// - Row version: two staff members never publish one draft twice
// =========================================================
public sealed class PlatformLegalDocument : AuditableEntity
{
    private readonly List<PlatformLegalDocumentPart> _parts = new();

    private PlatformLegalDocument()
    {
        Version = string.Empty;
    }

    private PlatformLegalDocument(PlatformLegalDocumentKind kind, string version, Guid actorUserId, DateTimeOffset utcNow)
        : base(utcNow)
    {
        Kind = kind;
        Version = version;
        Status = PlatformLegalDocumentStatus.Draft;
        AcceptanceRequired = kind == PlatformLegalDocumentKind.Terms;
        CreatedByUserId = actorUserId;
    }

    public PlatformLegalDocumentKind Kind { get; private set; }

    public string Version { get; private set; }

    public PlatformLegalDocumentStatus Status { get; private set; }

    // Terms only: false means "notice only" (D-Lg4).
    public bool AcceptanceRequired { get; private set; }

    public DateTimeOffset? EffectiveUtc { get; private set; }

    // Acceptance required: the effective date (D-Lg8).
    public DateTimeOffset? DeadlineUtc { get; private set; }

    public DateTimeOffset? PublishedUtc { get; private set; }

    public Guid? PublishedByUserId { get; private set; }

    public Guid CreatedByUserId { get; private set; }

    public IReadOnlyList<PlatformLegalDocumentPart> Parts => _parts;

    public bool IsDraft => Status == PlatformLegalDocumentStatus.Draft;

    public bool NeedsAcceptance => Kind == PlatformLegalDocumentKind.Terms && AcceptanceRequired;

    public static PlatformLegalDocument CreateDraft(PlatformLegalDocumentKind kind, string? version, Guid actorUserId, DateTimeOffset utcNow)
    {
        if (!Enum.IsDefined(kind))
        {
            throw new DomainException(PlatformLegalErrors.KindInvalid);
        }

        if (actorUserId == Guid.Empty)
        {
            throw new DomainException(PlatformLegalErrors.ActorRequired);
        }

        return new PlatformLegalDocument(kind, Label(version), actorUserId, utcNow);
    }

    // Effective date null: 30 days from publish for acceptance-required Terms, at publish otherwise.
    public void UpdateDraft(string? version, bool acceptanceRequired, DateTimeOffset? effectiveUtc, DateTimeOffset utcNow)
    {
        EnsureDraft();
        Version = Label(version);
        AcceptanceRequired = Kind == PlatformLegalDocumentKind.Terms && acceptanceRequired;
        EffectiveUtc = effectiveUtc?.ToUniversalTime();
        MarkAsModified(utcNow);
    }

    public void SetParts(IEnumerable<PlatformLegalDocument> included, DateTimeOffset utcNow)
    {
        ArgumentNullException.ThrowIfNull(included);
        EnsureDraft();
        PlatformLegalDocument[] parts = included.ToArray();

        if (Kind != PlatformLegalDocumentKind.Terms
            || parts.Any(part => part.Kind == PlatformLegalDocumentKind.Terms || part.Status != PlatformLegalDocumentStatus.Published)
            || parts.GroupBy(part => part.Kind).Any(group => group.Count() > 1))
        {
            throw new DomainException(PlatformLegalErrors.PartsInvalid);
        }

        _parts.Clear();
        _parts.AddRange(parts.OrderBy(part => part.Kind).Select(part => new PlatformLegalDocumentPart(part.Id, part.Kind, part.Version)));
        MarkAsModified(utcNow);
    }

    // languagesWithFiles: the languages that have an uploaded file; all must be present.
    public void Publish(IEnumerable<string> languagesWithFiles, IEnumerable<PlatformLegalDocumentKind> publishedKinds, Guid actorUserId, DateTimeOffset utcNow)
    {
        ArgumentNullException.ThrowIfNull(languagesWithFiles);
        ArgumentNullException.ThrowIfNull(publishedKinds);
        EnsureDraft();

        if (actorUserId == Guid.Empty)
        {
            throw new DomainException(PlatformLegalErrors.ActorRequired);
        }

        HashSet<string> languages = languagesWithFiles.ToHashSet(StringComparer.Ordinal);

        if (Vixly.Shared.Kernel.Localization.LanguageCode.All.Any(language => !languages.Contains(language.Value)))
        {
            throw new DomainException(PlatformLegalErrors.LanguagesMissing);
        }

        // A Terms version includes every other kind that has a published version; Privacy and DPA at least.
        if (Kind == PlatformLegalDocumentKind.Terms)
        {
            HashSet<PlatformLegalDocumentKind> needed = publishedKinds.Where(kind => kind != PlatformLegalDocumentKind.Terms).ToHashSet();
            needed.Add(PlatformLegalDocumentKind.Privacy);
            needed.Add(PlatformLegalDocumentKind.DataProcessing);

            if (needed.Any(kind => _parts.All(part => part.Kind != kind)))
            {
                throw new DomainException(PlatformLegalErrors.PartsInvalid);
            }
        }

        DateTimeOffset effective = EffectiveUtc ?? (NeedsAcceptance ? utcNow.AddDays(PlatformLegalLimits.NoticeDays) : utcNow);

        if (effective < utcNow.AddMinutes(-5) || (NeedsAcceptance && effective < utcNow.AddDays(PlatformLegalLimits.NoticeDays).AddMinutes(-5)))
        {
            throw new DomainException(PlatformLegalErrors.EffectiveDateInvalid);
        }

        Status = PlatformLegalDocumentStatus.Published;
        EffectiveUtc = effective;
        DeadlineUtc = NeedsAcceptance ? effective : null;
        PublishedUtc = utcNow;
        PublishedByUserId = actorUserId;
        MarkAsModified(utcNow);
    }

    public void Supersede(DateTimeOffset utcNow)
    {
        if (Status == PlatformLegalDocumentStatus.Published)
        {
            Status = PlatformLegalDocumentStatus.Superseded;
            MarkAsModified(utcNow);
        }
    }

    private void EnsureDraft()
    {
        if (!IsDraft)
        {
            throw new DomainException(PlatformLegalErrors.NotDraft);
        }
    }

    private static string Label(string? version)
    {
        string text = version?.Trim() ?? string.Empty;

        if (text.Length == 0 || text.Length > PlatformLegalLimits.VersionMaxLength || text.Any(character => char.IsWhiteSpace(character) || char.IsControl(character)))
        {
            throw new DomainException(PlatformLegalErrors.VersionInvalid);
        }

        return text;
    }
}
