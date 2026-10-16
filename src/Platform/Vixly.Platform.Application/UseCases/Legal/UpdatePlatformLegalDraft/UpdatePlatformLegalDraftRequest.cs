namespace Vixly.Platform.Application.UseCases.Legal.UpdatePlatformLegalDraft;

// =========================================================
// FILE: UpdatePlatformLegalDraftRequest.cs
//
// PURPOSE:
// Changes a draft: version label, acceptance rule, effective date and included versions.
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class UpdatePlatformLegalDraftRequest
{
    public Guid DocumentId { get; init; }

    public string? Version { get; init; }

    public bool AcceptanceRequired { get; init; }

    public DateTimeOffset? EffectiveUtc { get; init; }

    public IReadOnlyList<Guid> IncludedDocumentIds { get; init; } = Array.Empty<Guid>();
}
