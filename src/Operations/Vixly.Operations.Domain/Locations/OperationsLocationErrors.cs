using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Domain.Locations;

// =========================================================
// FILE: OperationsLocationErrors.cs
//
// PURPOSE:
// Error codes of branches, warehouses and country contacts (Slice 1B).
//
// RESPONSIBILITIES:
// - Declare each refusal as a code
//
// IMPORTANT NOTES:
// - Codes start ops.locations.
// =========================================================
public static class OperationsLocationErrors
{
    public static Error CountryInvalid { get; } = Error.Create("ops.locations.country_invalid");

    public static Error AddressRequired { get; } = Error.Create("ops.locations.address_required");

    public static Error AddressTooLong { get; } = Error.Create("ops.locations.address_too_long", ("maxLength", OperationsLocationLimits.AddressLineMaxLength));

    public static Error CityRequired { get; } = Error.Create("ops.locations.city_required");

    public static Error CityTooLong { get; } = Error.Create("ops.locations.city_too_long", ("maxLength", OperationsLocationLimits.CityMaxLength));

    public static Error RegionTooLong { get; } = Error.Create("ops.locations.region_too_long", ("maxLength", OperationsLocationLimits.RegionMaxLength));

    public static Error PostalCodeTooLong { get; } = Error.Create("ops.locations.postal_code_too_long", ("maxLength", OperationsLocationLimits.PostalCodeMaxLength));

    public static Error CodeInvalid { get; } = Error.Create("ops.locations.code_invalid");

    public static Error NameRequired { get; } = Error.Create("ops.locations.name_required");

    public static Error NameTooLong { get; } = Error.Create("ops.locations.name_too_long", ("maxLength", OperationsLocationLimits.NameMaxLength));

    public static Error HoursTooLong { get; } = Error.Create("ops.locations.hours_too_long", ("maxLength", OperationsLocationLimits.HoursMaxLength));

    public static Error EmailInvalid { get; } = Error.Create("ops.locations.email_invalid");

    public static Error PhoneInvalid { get; } = Error.Create("ops.locations.phone_invalid");

    public static Error PhonesRequired { get; } = Error.Create("ops.locations.phones_required");

    public static Error TooManyPhones { get; } = Error.Create("ops.locations.too_many_phones", ("maximum", OperationsLocationLimits.MaxPhonesPerContact));

    public static Error PhoneNotValid(string phone) => Error.Create("ops.locations.phone_not_valid", ("phone", phone));

    public static Error RoleRequired { get; } = Error.Create("ops.locations.role_required");

    public static Error BranchRequired { get; } = Error.Create("ops.locations.branch_required");

    public static Error CodeTaken { get; } = Error.Create("ops.locations.code_taken");

    public static Error LabelTooLong { get; } = Error.Create("ops.locations.label_too_long", ("maxLength", OperationsLocationLimits.LabelMaxLength));
}
