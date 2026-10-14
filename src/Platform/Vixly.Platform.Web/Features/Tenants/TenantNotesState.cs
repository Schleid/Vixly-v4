using Vixly.Platform.Application.Queries;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Platform.Web.Features.Tenants;

// =========================================================
// FILE: TenantNotesState.cs
//
// PURPOSE:
// State of the notes section.
//
// RESPONSIBILITIES:
// - Hold one page of notes and the outcome of adding one
//
// IMPORTANT NOTES:
// - Immutable; the presenter replaces it
// =========================================================
public sealed record TenantNotesState(
    IReadOnlyList<PlatformTenantNoteItem> Notes,
    int Page,
    int TotalPages,
    bool Loaded,
    string? DoneKey,
    Error? Error)
{
    public static TenantNotesState Initial { get; } = new(Array.Empty<PlatformTenantNoteItem>(), 1, 0, false, null, null);
}
