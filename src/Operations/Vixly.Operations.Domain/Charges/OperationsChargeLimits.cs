using Vixly.Shared.Kernel.Domain;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Domain.Charges;

// =========================================================
// FILE: OperationsChargeLimits.cs
//
// PURPOSE:
// Sizes and checks of prices (Slice 4).
//
// RESPONSIBILITIES:
// - Keep the columns and the forms in step
// - Check amounts, percentages and short texts
//
// IMPORTANT NOTES:
// - Amounts are in the company's currency: up to 1,000,000 with two decimals; unit prices four
// =========================================================
public static class OperationsChargeLimits
{
    public const int ServiceCodeMaxLength = 40;

    public const int NameMaxLength = 80;

    public const int WordsMaxLength = 500;

    public const int ReasonMaxLength = 300;

    public const int MaxCards = 300;

    // A technical bound only: a company sets as many bands as it wants (owner, 2026-10-19).
    public const int MaxBands = 100;

    public const int MaxVersions = 500;

    public const int MaxItemTypes = 200;

    public const int MaxItems = 30;

    public const int MaxExtras = 100;

    public const int MaxInsuranceBands = 30;

    public const int MaxPieces = 50;

    public const decimal MaxAmount = 1_000_000m;

    // An amount of money: 0 to 1,000,000, at most two decimals.
    public static decimal Money(decimal value, Error error) =>
        value is >= 0 and <= MaxAmount && decimal.Round(value, 2) == value ? value : throw new DomainException(error);

    public static decimal? Money(decimal? value, Error error) => value is { } amount ? Money(amount, error) : null;

    // A price per unit: 0 to 1,000,000, at most four decimals.
    public static decimal UnitPrice(decimal value, Error error) =>
        value is >= 0 and <= MaxAmount && decimal.Round(value, 4) == value ? value : throw new DomainException(error);

    // A percentage: 0 to 100, at most three decimals.
    public static decimal Percent(decimal value, Error error) =>
        value is >= 0 and <= 100 && decimal.Round(value, 3) == value ? value : throw new DomainException(error);

    // A short text shown on a page: trimmed, without markup or control characters.
    public static string? Text(string? value, int maxLength, Error error)
    {
        string? text = string.IsNullOrWhiteSpace(value) ? null : value.Trim();

        return text is null || (text.Length <= maxLength && !text.Any(character => char.IsControl(character) || character is '<' or '>'))
            ? text
            : throw new DomainException(error);
    }
}
