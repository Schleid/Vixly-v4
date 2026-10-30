namespace Vixly.Operations.Domain.Settings;

// =========================================================
// FILE: OperationsSettingsLimits.cs
//
// PURPOSE:
// Sizes for the tenant settings.
//
// RESPONSIBILITIES:
// - One place for column sizes and rules
//
// IMPORTANT NOTES:
// - Database sizes come from here
// =========================================================
public static class OperationsSettingsLimits
{
    public const int CurrencyCodeLength = 3;

    public const int PrefixMaxLength = 6;
}
