using Vixly.Platform.Domain.Identity;

namespace Vixly.Platform.Application.Queries;

// =========================================================
// FILE: PlatformUserListFilter.cs
//
// PURPOSE:
// Filters for the staff user list.
//
// RESPONSIBILITIES:
// - Carry the search text and the status to show
//
// IMPORTANT NOTES:
// - Search matches first name, last name or email
// - Search is trimmed and clipped to 100 characters by the use case
// =========================================================
public sealed record PlatformUserListFilter(string? Search, PlatformUserAccountStatus? Status);
