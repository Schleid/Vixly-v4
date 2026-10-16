namespace Vixly.Platform.Application.UseCases.TenantAccount;

// =========================================================
// FILE: PlatformTenantAccountLimits.cs
//
// PURPOSE:
// Limits of the internal calls (Slice 9, D-K6).
//
// RESPONSIBILITIES:
// - Batch and page sizes
//
// IMPORTANT NOTES:
// - A batch read takes at most 100 tenants or slugs
// =========================================================
public static class PlatformTenantAccountLimits
{
    public const int BatchMax = 100;

    public const int PageSizeMax = 100;

    public static int PageSize(int requested) => Math.Clamp(requested, 1, PageSizeMax);
}
