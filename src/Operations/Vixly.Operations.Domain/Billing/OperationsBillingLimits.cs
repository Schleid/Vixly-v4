using Vixly.Shared.Kernel.Domain;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Domain.Billing;

// =========================================================
// FILE: OperationsBillingLimits.cs
//
// PURPOSE:
// Sizes and rounding of invoices (Slice 5F step 1).
//
// RESPONSIBILITIES:
// - Bound texts and rates
// - Round money to the cent
//
// IMPORTANT NOTES:
// - Money is rounded half away from zero to the cent, as the rating engine does
// =========================================================
public static class OperationsBillingLimits
{
    public const int ReasonMaxLength = 500;

    public const int TaxNameMaxLength = 40;

    public const int TaxNumberMaxLength = 40;

    public const int NoteMaxLength = 1000;

    public const decimal MaxTaxPercent = 100m;

    public const int MaxLines = 200;

    public static decimal Money(decimal amount) => decimal.Round(amount, 2, MidpointRounding.AwayFromZero);

    // Trimmed; empty is null; longer than the limit or with control characters is refused.
    public static string? Text(string? value, int maxLength, Error refused)
    {
        string? text = string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        return text is null ? null
            : text.Length > maxLength || text.Any(character => char.IsControl(character) && character is not '\r' and not '\n') ? throw new DomainException(refused)
            : text;
    }
}
