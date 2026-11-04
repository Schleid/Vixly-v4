using Vixly.Operations.Domain.Texts;

namespace Vixly.Operations.Application.UseCases.Texts.SaveOperationsTextNotices;

// =========================================================
// FILE: SaveOperationsTextNoticesRequest.cs
//
// PURPOSE:
// The notices the company sends by text, and its WhatsApp templates (Slice 9).
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class SaveOperationsTextNoticesRequest
{
    public OperationsTextNotices Notices { get; init; }

    // The approved template (HX...) for each notice sent on WhatsApp; empty: none.
    public IReadOnlyDictionary<OperationsTextNotices, string?> Templates { get; init; } = new Dictionary<OperationsTextNotices, string?>();
}
