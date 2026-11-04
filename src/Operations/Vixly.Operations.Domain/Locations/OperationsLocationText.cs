using System.Text.RegularExpressions;
using Vixly.Shared.Kernel.Domain;
using Vixly.Shared.Kernel.Errors;
using Vixly.Shared.Kernel.ValueObjects;

namespace Vixly.Operations.Domain.Locations;

// =========================================================
// FILE: OperationsLocationText.cs
//
// PURPOSE:
// Validation of free-text location fields.
//
// RESPONSIBILITIES:
// - Trim, check lengths, codes, phones and emails
//
// IMPORTANT NOTES:
// - Internal: only location entities use it
// =========================================================
internal static class OperationsLocationText
{
    private static readonly Regex CodePattern = new("^[A-Z0-9][A-Z0-9-]{1,11}$", RegexOptions.CultureInvariant | RegexOptions.Compiled);

    public static string Required(string? value, int maxLength, Error missing, Error tooLong)
    {
        string trimmed = value?.Trim() ?? string.Empty;

        if (trimmed.Length == 0)
        {
            throw new DomainException(missing);
        }

        return trimmed.Length > maxLength ? throw new DomainException(tooLong) : trimmed;
    }

    public static string? Optional(string? value, int maxLength, Error tooLong)
    {
        string? trimmed = string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        return trimmed is not null && trimmed.Length > maxLength ? throw new DomainException(tooLong) : trimmed;
    }

    public static string Code(string? value)
    {
        string code = (value ?? string.Empty).Trim().ToUpperInvariant();
        return CodePattern.IsMatch(code) ? code : throw new DomainException(OperationsLocationErrors.CodeInvalid);
    }

    public static string? Phone(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return PhoneNumber.TryCreate(value, out PhoneNumber? phone) ? phone!.Value : throw new DomainException(OperationsLocationErrors.PhoneInvalid);
    }

    public static string? Email(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return EmailAddress.TryCreate(value, out EmailAddress? email) ? email!.Value : throw new DomainException(OperationsLocationErrors.EmailInvalid);
    }
}
