using Vixly.Platform.Application.UseCases.Legal;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Platform.Web.Features.Legal;

// =========================================================
// FILE: LegalDocumentState.cs
//
// PURPOSE:
// What the legal document page shows.
//
// RESPONSIBILITIES:
// - Hold the version, its files, included versions, the last result and a waiting step-up
//
// IMPORTANT NOTES:
// - Immutable; Version changes on every load so the form refills
// =========================================================
public sealed record LegalDocumentState(PlatformLegalDocumentDetail? Detail, bool Loaded, int Version, string? DoneKey, bool StepUpPending, bool Deleted, Error? Error)
{
    public static LegalDocumentState Initial { get; } = new(null, false, 0, null, false, false, null);
}
