using Vixly.Platform.Application.Queries;
using Vixly.Platform.Application.UseCases.Licensing;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Platform.Web.Features.Licensing;

// =========================================================
// FILE: TenantLicensingState.cs
//
// PURPOSE:
// What the Licensing section of the tenant page shows.
//
// RESPONSIBILITIES:
// - Hold the seat summary, the last result and a waiting step-up action
//
// IMPORTANT NOTES:
// - Immutable; a new state per change
// =========================================================
public sealed record TenantLicensingState(PlatformTenantLicensingSummary? Summary, bool Loaded, string? DoneKey, string? StepUpAction, Error? Error)
{
    public static TenantLicensingState Initial { get; } = new(null, false, null, null, null);
}
