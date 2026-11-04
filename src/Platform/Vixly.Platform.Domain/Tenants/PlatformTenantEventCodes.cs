namespace Vixly.Platform.Domain.Tenants;

// =========================================================
// FILE: PlatformTenantEventCodes.cs
//
// PURPOSE:
// The codes of a tenant's history events (D-4).
//
// RESPONSIBILITIES:
// - Name every event the tenant aggregate records
//
// IMPORTANT NOTES:
// - History is code plus arguments, so it reads in the
//   reader's language (V3 stored English sentences)
// - Each code needs en, fr and es text under ui.tenant_history.
// - Never reword a code that has shipped. Add a new one
// =========================================================
public static class PlatformTenantEventCodes
{
    public const string CreatedByStaff = "platform.tenant_history.created_by_staff";

    public const string CreatedFromRegistration = "platform.tenant_history.created_from_registration";

    public const string TermsAccepted = "platform.tenant_history.terms_accepted";

    public const string Activated = "platform.tenant_history.activated";

    public const string Suspended = "platform.tenant_history.suspended";

    public const string Reactivated = "platform.tenant_history.reactivated";

    public const string Deactivated = "platform.tenant_history.deactivated";

    public const string Banned = "platform.tenant_history.banned";

    public const string SlugCorrected = "platform.tenant_history.slug_corrected";

    public const string CompanyChanged = "platform.tenant_history.company_changed";

    public static IReadOnlyList<string> All { get; } = new[]
    {
        CreatedByStaff, CreatedFromRegistration, TermsAccepted, Activated, Suspended,
        Reactivated, Deactivated, Banned, SlugCorrected, CompanyChanged
    };
}
