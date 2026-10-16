using Vixly.Platform.Domain.Legal;

namespace Vixly.Platform.Application.Queries;

// =========================================================
// FILE: PlatformLegalRegisterFilter.cs
//
// PURPOSE:
// Which tenants the acceptance register shows.
//
// RESPONSIBILITIES:
// - Filter by legal status and search by name or code
//
// IMPORTANT NOTES:
// - Only tenants that must accept are ever listed
// =========================================================
public sealed record PlatformLegalRegisterFilter(PlatformLegalStatus? Status, string? Search);
