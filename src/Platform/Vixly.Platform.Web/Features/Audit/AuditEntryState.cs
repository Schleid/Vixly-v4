using Vixly.Platform.Application.Queries;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Platform.Web.Features.Audit;

// =========================================================
// FILE: AuditEntryState.cs
//
// PURPOSE:
// What the audit entry page shows.
//
// RESPONSIBILITIES:
// - Hold the entry and any error
//
// IMPORTANT NOTES:
// - Immutable; the presenter replaces it
// =========================================================
public sealed record AuditEntryState(PlatformAuditEntryDetail? Entry, Error? Error)
{
    public static AuditEntryState Initial { get; } = new(null, null);
}
