using System.Net.Mail;
using Vixly.Shared.Kernel.Domain;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Shared.Kernel.ValueObjects;

// =========================================================
// FILE: EmailAddress.cs
//
// PURPOSE:
// A validated, normalized email address.
//
// RESPONSIBILITIES:
// - Trim and lower-case input
// - Reject malformed addresses and over-long values
// - Provide value-based equality
//
// IMPORTANT NOTES:
// - Validates format only; it does not verify the mailbox
//   exists or that the owner consented
// - New values are always validated. Persisted legacy data is
//   cleaned by the migration tool, never loaded unvalidated
//   (ADR-0002)
// =========================================================
public sealed record EmailAddress
{
    public const int MaxLength = 254;

    private EmailAddress(string value)
    {
        Value = value;
    }

    public string Value { get; }

    // =====================================================
    // METHOD: CREATE
    // =====================================================
    public static EmailAddress Create(string? value)
    {
        if (!TryCreate(value, out EmailAddress? email))
        {
            throw new DomainException(KernelErrors.EmailInvalid);
        }

        return email;
    }

    // =====================================================
    // METHOD: TRY CREATE
    // =====================================================
    public static bool TryCreate(
        string? value,
        [System.Diagnostics.CodeAnalysis.NotNullWhen(true)] out EmailAddress? email)
    {
        email = null;

        string normalized = (value ?? string.Empty).Trim().ToLowerInvariant();

        if (normalized.Length == 0
            || normalized.Length > MaxLength
            || !IsValid(normalized))
        {
            return false;
        }

        email = new EmailAddress(normalized);
        return true;
    }

    public override string ToString()
    {
        return Value;
    }

    // =====================================================
    // METHOD: IS VALID
    // =====================================================
    private static bool IsValid(string value)
    {
        if (value.Any(char.IsWhiteSpace))
        {
            return false;
        }

        if (!MailAddress.TryCreate(value, out MailAddress? address)
            || !string.Equals(address.Address, value, StringComparison.Ordinal))
        {
            return false;
        }

        int separatorIndex = value.LastIndexOf('@');

        if (separatorIndex <= 0 || separatorIndex == value.Length - 1)
        {
            return false;
        }

        string[] labels = value[(separatorIndex + 1)..].Split('.');

        return labels.Length >= 2
            && labels.All(IsValidDomainLabel)
            && labels[^1].Length >= 2
            && labels[^1].All(char.IsLetter);
    }

    // =====================================================
    // METHOD: IS VALID DOMAIN LABEL
    // =====================================================
    private static bool IsValidDomainLabel(string label)
    {
        return label.Length is >= 1 and <= 63
            && char.IsLetterOrDigit(label[0])
            && char.IsLetterOrDigit(label[^1])
            && label.All(character => char.IsLetterOrDigit(character) || character == '-');
    }
}
