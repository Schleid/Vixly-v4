using Vixly.Platform.Application.Queries;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Platform.Web.Features.Tenants;

// =========================================================
// FILE: TenantManagementState.cs
//
// PURPOSE:
// State of the account management section.
//
// RESPONSIBILITIES:
// - Hold whether the form is open and the outcome
//
// IMPORTANT NOTES:
// - Immutable; the presenter replaces it
// =========================================================
public sealed record TenantManagementState(bool Editing, IReadOnlyList<PlatformStaffOptionItem> Staff, string? DoneKey, Error? Error)
{
    public static TenantManagementState Initial { get; } = new(false, Array.Empty<PlatformStaffOptionItem>(), null, null);
}
