using Vixly.Shared.Kernel.Domain;

namespace Vixly.Platform.Domain.Subscriptions;

// =========================================================
// FILE: PlatformPlanCodeFormat.cs
//
// PURPOSE:
// A plan's code: short, upper case and permanent.
//
// RESPONSIBILITIES:
// - Normalise and validate a plan code
//
// IMPORTANT NOTES:
// - 2 to 32 characters: letters, digits, hyphen and underscore
// - Stored upper case (V3 rule); never changes after creation
// =========================================================
public static class PlatformPlanCodeFormat
{
    public static string Normalize(string? code)
    {
        string value = code?.Trim().ToUpperInvariant() ?? string.Empty;

        if (value.Length < PlatformSubscriptionLimits.PlanCodeMinLength
            || value.Length > PlatformSubscriptionLimits.PlanCodeMaxLength
            || !value.All(character => char.IsAsciiLetterUpper(character) || char.IsAsciiDigit(character) || character is '-' or '_')
            || !char.IsAsciiLetterOrDigit(value[0]))
        {
            throw new DomainException(PlatformPlanErrors.CodeInvalid);
        }

        return value;
    }
}
