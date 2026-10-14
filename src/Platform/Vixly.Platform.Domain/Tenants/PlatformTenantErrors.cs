using Vixly.Shared.Kernel.Errors;

namespace Vixly.Platform.Domain.Tenants;

// =========================================================
// FILE: PlatformTenantErrors.cs
//
// PURPOSE:
// The errors raised by the tenant model.
//
// RESPONSIBILITIES:
// - Declare every error code of this area in one place
//
// IMPORTANT NOTES:
// - Codes start with platform.tenants. (Global Standard 4.2)
// - Each code needs en, fr and es text (L-10)
// - Never reword a code that has shipped. Add a new one
// =========================================================
public static class PlatformTenantErrors
{
    public static Error LegalNameRequired { get; } =
        Error.Create("platform.tenants.legal_name_required");

    public static Error LegalNameTooLong { get; } =
        Error.Create("platform.tenants.legal_name_too_long", ("maxLength", PlatformTenantLimits.CompanyNameMaxLength));

    public static Error TradingNameTooLong { get; } =
        Error.Create("platform.tenants.trading_name_too_long", ("maxLength", PlatformTenantLimits.CompanyNameMaxLength));

    public static Error AddressRequired { get; } =
        Error.Create("platform.tenants.address_required");

    public static Error AddressTooLong { get; } =
        Error.Create("platform.tenants.address_too_long", ("maxLength", PlatformTenantLimits.AddressLineMaxLength));

    public static Error CityRequired { get; } =
        Error.Create("platform.tenants.city_required");

    public static Error CityTooLong { get; } =
        Error.Create("platform.tenants.city_too_long", ("maxLength", PlatformTenantLimits.CityMaxLength));

    public static Error StateProvinceTooLong { get; } =
        Error.Create("platform.tenants.state_province_too_long", ("maxLength", PlatformTenantLimits.StateProvinceMaxLength));

    public static Error PostalCodeTooLong { get; } =
        Error.Create("platform.tenants.postal_code_too_long", ("maxLength", PlatformTenantLimits.PostalCodeMaxLength));

    public static Error CountryInvalid { get; } =
        Error.Create("platform.tenants.country_invalid");

    public static Error TimeZoneInvalid { get; } =
        Error.Create("platform.tenants.time_zone_invalid");

    public static Error CodeInvalid { get; } =
        Error.Create("platform.tenants.code_invalid");

    public static Error SlugInvalid { get; } =
        Error.Create("platform.tenants.slug_invalid", ("minLength", PlatformTenantLimits.SlugMinLength), ("maxLength", PlatformTenantLimits.SlugMaxLength));

    public static Error SlugReserved { get; } =
        Error.Create("platform.tenants.slug_reserved");

    public static Error SlugTaken { get; } =
        Error.Create("platform.tenants.slug_taken");

    public static Error SlugUnchanged { get; } =
        Error.Create("platform.tenants.slug_unchanged");

    public static Error SlugFixed { get; } =
        Error.Create("platform.tenants.slug_fixed");

    public static Error ContactNameRequired { get; } =
        Error.Create("platform.tenants.contact_name_required");

    public static Error ContactNameTooLong { get; } =
        Error.Create("platform.tenants.contact_name_too_long", ("maxLength", PlatformTenantLimits.ContactNameMaxLength));

    public static Error ContactRoleInvalid { get; } =
        Error.Create("platform.tenants.contact_role_invalid");

    public static Error OwnerContactRequired { get; } =
        Error.Create("platform.tenants.owner_contact_required");

    public static Error ContactRoleDuplicate { get; } =
        Error.Create("platform.tenants.contact_role_duplicate");

    public static Error TagInvalid { get; } =
        Error.Create("platform.tenants.tag_invalid", ("minLength", PlatformTenantLimits.TagMinLength), ("maxLength", PlatformTenantLimits.TagMaxLength));

    public static Error TooManyTags { get; } =
        Error.Create("platform.tenants.too_many_tags", ("maxCount", PlatformTenantLimits.MaxTagsPerTenant));

    public static Error NoteRequired { get; } =
        Error.Create("platform.tenants.note_required");

    public static Error NoteTooLong { get; } =
        Error.Create("platform.tenants.note_too_long", ("maxLength", PlatformTenantLimits.NoteMaxLength));

    public static Error ReasonRequired { get; } =
        Error.Create("platform.tenants.reason_required");

    public static Error ReasonTooLong { get; } =
        Error.Create("platform.tenants.reason_too_long", ("maxLength", PlatformTenantLimits.ReasonMaxLength));

    public static Error SuspensionCategoryInvalid { get; } =
        Error.Create("platform.tenants.suspension_category_invalid");

    public static Error AlreadyBanned { get; } =
        Error.Create("platform.tenants.already_banned");

    public static Error TenantRequired { get; } =
        Error.Create("platform.tenants.tenant_required");

    public static Error ActorRequired { get; } =
        Error.Create("platform.tenants.actor_required");

    public static Error MoveNotAllowed(PlatformTenantStatus from, PlatformTenantStatus to)
    {
        return Error.Create(
            "platform.tenants.move_not_allowed",
            ("from", from.ToString()),
            ("to", to.ToString()));
    }

    public static Error AccountManagerInvalid { get; } =
        Error.Create("platform.tenants.account_manager_invalid");
}
