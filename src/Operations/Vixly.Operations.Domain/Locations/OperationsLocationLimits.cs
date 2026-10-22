namespace Vixly.Operations.Domain.Locations;

// =========================================================
// FILE: OperationsLocationLimits.cs
//
// PURPOSE:
// Sizes of location fields.
//
// RESPONSIBILITIES:
// - Name every limit
//
// IMPORTANT NOTES:
// - Used by the domain and the database mapping
// =========================================================
public static class OperationsLocationLimits
{
    public const int AddressLineMaxLength = 200;

    public const int CityMaxLength = 100;

    public const int RegionMaxLength = 100;

    public const int PostalCodeMaxLength = 20;

    public const int CodeMaxLength = 12;

    public const int NameMaxLength = 100;

    public const int HoursMaxLength = 200;

    public const int LabelMaxLength = 60;

    public const int EmailMaxLength = 254;

    public const int PhoneMaxLength = 20;

    // Phone numbers of one country contact: several lines that all reach the same call centre.
    public const int MaxPhonesPerContact = 12;
}
