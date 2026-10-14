namespace Vixly.Platform.Application.Queries;

// =========================================================
// FILE: PlatformTenantNoteItem.cs
//
// PURPOSE:
// One internal note about a tenant.
//
// RESPONSIBILITIES:
// - Carry read-only data to the console
//
// IMPORTANT NOTES:
// - Never sent to the tenant
// =========================================================
public sealed record PlatformTenantNoteItem(
    Guid NoteId,
    string Body,
    string? AuthorName,
    DateTimeOffset CreatedUtc);
