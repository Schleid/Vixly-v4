using Vixly.Shared.Kernel.Errors;

namespace Vixly.Platform.Application.UseCases.Tenants;

// =========================================================
// FILE: PlatformTenantAdminErrors.cs
//
// PURPOSE:
// Errors raised by tenant use cases rather than the tenant model.
//
// RESPONSIBILITIES:
// - Declare the codes the use cases add
//
// IMPORTANT NOTES:
// - Codes start with platform.tenants. (Global Standard 4.2)
// - Each code needs en, fr and es text (L-10)
// =========================================================
public static class PlatformTenantAdminErrors
{
    public static Error BanNeedsSuperUser { get; } =
        Error.Create("platform.tenants.ban_needs_superuser");

    public static Error CodeUnavailable { get; } =
        Error.Create("platform.tenants.code_unavailable");
}
