using Vixly.Shared.Kernel.Errors;

namespace Vixly.Platform.Web.Features.Tenants;

// =========================================================
// FILE: TenantContactsState.cs
//
// PURPOSE:
// State of the contacts section.
//
// RESPONSIBILITIES:
// - Hold whether the form is open and the outcome
//
// IMPORTANT NOTES:
// - Immutable; the presenter replaces it
// =========================================================
public sealed record TenantContactsState(bool Editing, string? DoneKey, Error? Error)
{
    public static TenantContactsState Initial { get; } = new(false, null, null);
}
