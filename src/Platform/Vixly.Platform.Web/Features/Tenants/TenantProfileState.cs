using Vixly.Shared.Kernel.Errors;

namespace Vixly.Platform.Web.Features.Tenants;

// =========================================================
// FILE: TenantProfileState.cs
//
// PURPOSE:
// State of the company profile section.
//
// RESPONSIBILITIES:
// - Hold whether the form is open and the outcome
//
// IMPORTANT NOTES:
// - Immutable; the presenter replaces it
// =========================================================
public sealed record TenantProfileState(bool Editing, string? DoneKey, Error? Error)
{
    public static TenantProfileState Initial { get; } = new(false, null, null);
}
