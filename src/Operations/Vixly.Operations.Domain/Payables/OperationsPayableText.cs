using Vixly.Shared.Kernel.Domain;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Domain.Payables;

// =========================================================
// FILE: OperationsPayableText.cs
//
// PURPOSE:
// Trims and checks the texts of bills and vendors (Slice 8).
//
// RESPONSIBILITIES:
// - Required and optional texts, money to the cent
//
// IMPORTANT NOTES:
// - No control characters; empty means none
// =========================================================
public static class OperationsPayableText
{
    public static string? Optional(string? value, int max)
    {
        string text = (value ?? string.Empty).Trim();

        if (text.Length > max || text.Any(character => char.IsControl(character) && character is not '\n' and not '\r'))
        {
            throw new DomainException(OperationsPayableErrors.TextInvalid);
        }

        return text.Length == 0 ? null : text;
    }

    public static string Required(string? value, int max, Error missing) => Optional(value, max) ?? throw new DomainException(missing);

    public static decimal Money(decimal amount, Error refused) =>
        amount > 0m && amount < 100_000_000m && decimal.Round(amount, 2) == amount ? amount : throw new DomainException(refused);
}
