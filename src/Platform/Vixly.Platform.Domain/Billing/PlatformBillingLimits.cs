namespace Vixly.Platform.Domain.Billing;

// =========================================================
// FILE: PlatformBillingLimits.cs
//
// PURPOSE:
// Field sizes and bounds for billing.
//
// RESPONSIBILITIES:
// - Keep every size in one place for the domain, the database and the screens
//
// IMPORTANT NOTES:
// - Not commercial values: those are Billing settings (D-B4)
// =========================================================
public static class PlatformBillingLimits
{
    public const int MoneyScale = 2;

    public const int RateScale = 4;

    public const decimal MaxRate = 100m;

    public const decimal MaxAmount = 100000000m;

    public const int NumberMaxLength = 32;

    public const int PrefixMaxLength = 8;

    public const int ReferenceMaxLength = 100;

    public const int NoteMaxLength = 500;

    public const int ReasonMaxLength = 500;

    public const int TaxLabelMaxLength = 40;

    public const int TaxIdMaxLength = 40;

    public const int RegionMaxLength = 64;

    public const int NameMaxLength = 200;

    public const int AddressMaxLength = 600;

    public const int EmailMaxLength = 254;

    public const int PhoneMaxLength = 32;

    public const int WebsiteMaxLength = 200;

    public const int TextKeyMaxLength = 100;

    public const int BillingTextMaxLength = 1000;

    public const int MaxPaymentTermsDays = 365;

    public const int MaxLogoKilobytes = 512;

    public const int MaxNumber = 999999;

    public const int SourceKeyMaxLength = 120;

    public static decimal Money(decimal amount)
    {
        return decimal.Round(amount, MoneyScale, MidpointRounding.AwayFromZero);
    }

    public static bool IsMoney(decimal amount)
    {
        return decimal.Round(amount, MoneyScale) == amount;
    }
}
