using Vixly.Shared.Kernel.Domain;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Domain.Receiving;

// =========================================================
// FILE: OperationsReceivingText.cs
//
// PURPOSE:
// Checks the free text and amounts of receiving (Slice 3B).
//
// RESPONSIBILITIES:
// - Trim, require, limit; refuse markup and links in text a customer or an email may show
//
// IMPORTANT NOTES:
// - Shared by pre-alerts and parcels
// =========================================================
public static class OperationsReceivingText
{
    public static string Required(string? value, int maxLength, Error error) =>
        Optional(value, maxLength) ?? throw new DomainException(error);

    public static string? Optional(string? value, int maxLength)
    {
        string? text = string.IsNullOrWhiteSpace(value) ? null : value.Trim();

        if (text is null)
        {
            return null;
        }

        return text.Length > maxLength || text.IndexOfAny(['<', '>']) >= 0 || text.Any(char.IsControl) || text.Contains("://", StringComparison.Ordinal)
            ? throw new DomainException(OperationsReceivingErrors.TextTooLong)
            : text;
    }

    public static decimal Value(decimal value) =>
        value >= 0 && value <= OperationsReceivingLimits.MaxDeclaredValue ? decimal.Round(value, 2) : throw new DomainException(OperationsReceivingErrors.ValueInvalid);
}
