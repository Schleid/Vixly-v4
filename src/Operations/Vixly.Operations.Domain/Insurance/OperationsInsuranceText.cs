using Vixly.Shared.Kernel.Domain;

namespace Vixly.Operations.Domain.Insurance;

// =========================================================
// FILE: OperationsInsuranceText.cs
//
// PURPOSE:
// Checks the words and amounts of insurance records (Slice 5B).
//
// RESPONSIBILITIES:
// - Trim, bound and refuse markup in texts; bound amounts and shares
//
// IMPORTANT NOTES:
// - Every refusal is DetailsInvalid or AmountInvalid
// =========================================================
public static class OperationsInsuranceText
{
    public const decimal MaxAmount = 1_000_000_000m;

    public static string Required(string? value, int maxLength) =>
        Optional(value, maxLength) ?? throw new DomainException(OperationsInsuranceErrors.DetailsInvalid);

    // Letters, digits and punctuation; several lines allowed for notes; never markup or links.
    public static string? Optional(string? value, int maxLength, bool lines = false)
    {
        string? text = string.IsNullOrWhiteSpace(value) ? null : value.Trim().Replace("\r\n", "\n", StringComparison.Ordinal);

        if (text is null)
        {
            return null;
        }

        return text.Length > maxLength || text.IndexOfAny(['<', '>']) >= 0 || text.Any(character => char.IsControl(character) && !(lines && character == '\n'))
            ? throw new DomainException(OperationsInsuranceErrors.DetailsInvalid)
            : text;
    }

    public static decimal? Amount(decimal? value) =>
        value is null ? null : value is >= 0 and <= MaxAmount ? decimal.Round(value.Value, 2) : throw new DomainException(OperationsInsuranceErrors.AmountInvalid);

    public static decimal? Percent(decimal? value) =>
        value is null ? null : value is >= 0 and <= 100 ? decimal.Round(value.Value, 3) : throw new DomainException(OperationsInsuranceErrors.AmountInvalid);
}
