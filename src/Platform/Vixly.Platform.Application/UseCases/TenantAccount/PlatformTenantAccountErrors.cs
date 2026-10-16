using Vixly.Shared.Kernel.Errors;

namespace Vixly.Platform.Application.UseCases.TenantAccount;

// =========================================================
// FILE: PlatformTenantAccountErrors.cs
//
// PURPOSE:
// Errors of the tenant's own account calls (Slice 9).
//
// RESPONSIBILITIES:
// - Name the refusals Operations can show the tenant
//
// IMPORTANT NOTES:
// - The owner contact is changed by Vixly staff only (identity and recovery)
// =========================================================
public static class PlatformTenantAccountErrors
{
    public static Error OwnerContactLocked { get; } =
        Error.Create("platform.tenant_account.owner_contact_locked");

    public static Error PlanNotOffered { get; } =
        Error.Create("platform.tenant_account.plan_not_offered");

    public static Error TooManyTenants { get; } =
        Error.Create("platform.tenant_account.too_many", ("max", PlatformTenantAccountLimits.BatchMax.ToString(System.Globalization.CultureInfo.InvariantCulture)));
}
