using Vixly.Platform.Application.Queries;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Platform.Web.Features.Sales;

// =========================================================
// FILE: LeadState.cs
//
// PURPOSE:
// What the lead page shows.
//
// RESPONSIBILITIES:
// - Hold the lead, whether the reader may update it and the last result
//
// IMPORTANT NOTES:
// - Immutable; a new state per change
// =========================================================
public sealed record LeadState(PlatformSalesLeadItem? Lead, bool CanManage, bool Loaded, bool Saved, Error? Error)
{
    public static LeadState Initial { get; } = new(null, false, false, false, null);
}
