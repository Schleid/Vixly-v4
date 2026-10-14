namespace Vixly.Platform.Domain.Tenants;

// =========================================================
// FILE: PlatformTenantLimits.cs
//
// PURPOSE:
// Field lengths, counts and time windows of the tenant model.
//
// RESPONSIBILITIES:
// - Hold the limits the domain enforces and EF maps
//
// IMPORTANT NOTES:
// - EF configurations read these values, so column sizes and
//   domain checks never disagree
// - Windows come from the Slice 2 design note
// =========================================================
public static class PlatformTenantLimits
{
    public const int TenantCodeMinLength = 3;

    public const int TenantCodeMaxLength = 50;

    public const int SlugMinLength = 3;

    public const int SlugMaxLength = 63;

    public const int CompanyNameMaxLength = 200;

    public const int AddressLineMaxLength = 200;

    public const int CityMaxLength = 100;

    public const int StateProvinceMaxLength = 100;

    public const int PostalCodeMaxLength = 20;

    public const int CountryCodeLength = 2;

    public const int TimeZoneMaxLength = 64;

    public const int ContactNameMaxLength = 200;

    public const int ReasonMaxLength = 500;

    public const int NoteMaxLength = 4000;

    public const int TagMinLength = 2;

    public const int TagMaxLength = 32;

    public const int MaxTagsPerTenant = 10;

    public const int EventCodeMaxLength = 100;

    public const int LegalVersionMaxLength = 50;

    public const int IpAddressMaxLength = 45;

    public const int MaxVerificationSendsPerHour = 3;

    public static readonly TimeSpan PendingSlugHold = TimeSpan.FromHours(24);

    public static readonly TimeSpan VerificationLinkLifetime = TimeSpan.FromHours(24);

    public static readonly TimeSpan RegistrationAccessLifetime = TimeSpan.FromDays(7);

    public static readonly TimeSpan PlanSelectionWindow = TimeSpan.FromDays(30);
}
