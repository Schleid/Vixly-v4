using Vixly.Platform.Application.UseCases.Collections;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Platform.Web.Features.Collections;

// =========================================================
// FILE: TenantCollectionsState.cs
//
// PURPOSE:
// What the Collections part of the tenant Billing section shows.
//
// RESPONSIBILITIES:
// - Hold the open case and overdue invoices
//
// IMPORTANT NOTES:
// - Immutable; a new state per change
// =========================================================
public sealed record TenantCollectionsState(PlatformTenantCollectionsSummary? Summary, bool Loaded, Error? Error)
{
    public static TenantCollectionsState Initial { get; } = new(null, false, null);
}
