using Vixly.Platform.Application.Queries;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Platform.Web.Features.Registrations;

// =========================================================
// FILE: RegistrationListState.cs
//
// PURPOSE:
// State of the registrations list.
//
// RESPONSIBILITIES:
// - Hold one page of registrations
//
// IMPORTANT NOTES:
// - Immutable; the presenter replaces it
// =========================================================
public sealed record RegistrationListState(
    IReadOnlyList<PlatformTenantRegistrationListItem> Registrations,
    int Page,
    int TotalPages,
    int TotalCount,
    bool Loaded,
    Error? Error)
{
    public static RegistrationListState Initial { get; } = new(Array.Empty<PlatformTenantRegistrationListItem>(), 1, 0, 0, false, null);
}
