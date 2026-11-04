using Vixly.Platform.Application.Queries;

namespace Vixly.Platform.Application.UseCases.Licensing;

// =========================================================
// FILE: PlatformTenantLicensingSummary.cs
//
// PURPOSE:
// The Licensing section of a tenant page (D-L15).
//
// RESPONSIBILITIES:
// - Carry seats bought, the scheduled number, devices by state, the allowance and abilities
//
// IMPORTANT NOTES:
// - Seats bought come from the subscription (Slice 3)
// =========================================================
public sealed record PlatformTenantLicensingSummary(
    int SeatsBought,
    int? SeatsScheduled,
    PlatformLicensingCounts Counts,
    int ReleaseAllowance,
    bool FirstDeviceLinkAllowed,
    PlatformLicensingAbilities Abilities,
    IReadOnlyList<PlatformOwnerSupportItem> Support,
    IReadOnlyList<PlatformDeviceOption> ActiveDevices);
