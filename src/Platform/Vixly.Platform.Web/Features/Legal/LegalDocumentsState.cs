using Vixly.Platform.Application.Queries;
using Vixly.Platform.Application.UseCases.Legal;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Platform.Web.Features.Legal;

// =========================================================
// FILE: LegalDocumentsState.cs
//
// PURPOSE:
// What the legal documents list shows.
//
// RESPONSIBILITIES:
// - Hold one page of versions and the reader's abilities
//
// IMPORTANT NOTES:
// - Immutable; a new state per change
// =========================================================
public sealed record LegalDocumentsState(IReadOnlyList<PlatformLegalDocumentItem> Items, int Page, int TotalPages, PlatformLegalAbilities Abilities, bool Loaded, Guid? CreatedId, Error? Error)
{
    public static LegalDocumentsState Initial { get; } = new(Array.Empty<PlatformLegalDocumentItem>(), 1, 0, PlatformLegalAbilities.None, false, null, null);
}
