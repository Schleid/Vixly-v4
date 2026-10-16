using Vixly.Platform.Domain.Legal;

namespace Vixly.Platform.Application.UseCases.Legal.CreatePlatformLegalDraft;

// =========================================================
// FILE: CreatePlatformLegalDraftRequest.cs
//
// PURPOSE:
// Starts a new version of a legal document as a draft.
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class CreatePlatformLegalDraftRequest
{
    public PlatformLegalDocumentKind Kind { get; init; }

    public string? Version { get; init; }
}
