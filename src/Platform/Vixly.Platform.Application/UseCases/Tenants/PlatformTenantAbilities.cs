namespace Vixly.Platform.Application.UseCases.Tenants;

// =========================================================
// FILE: PlatformTenantAbilities.cs
//
// PURPOSE:
// What the signed-in staff member may do to tenants.
//
// RESPONSIBILITIES:
// - Tell the console which actions to offer
//
// IMPORTANT NOTES:
// - The use cases check again: hiding a button is not security
// - Ban also needs SuperUser authority
// =========================================================
public sealed record PlatformTenantAbilities(
    bool CanEdit,
    bool CanAddNotes,
    bool CanActivate,
    bool CanSuspend,
    bool CanReactivate,
    bool CanDeactivate,
    bool CanBan)
{
    public static PlatformTenantAbilities None { get; } = new(false, false, false, false, false, false, false);
}
