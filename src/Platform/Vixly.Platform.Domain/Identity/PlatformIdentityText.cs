using Vixly.Shared.Kernel.Domain;

namespace Vixly.Platform.Domain.Identity;

// =========================================================
// FILE: PlatformIdentityText.cs
//
// PURPOSE:
// Shared validation for the free-text fields of the identity
// model.
//
// RESPONSIBILITIES:
// - Trim and validate required names
// - Trim optional values and turn blanks into null
// - Validate reasons for account actions
//
// IMPORTANT NOTES:
// - Internal: only identity entities use it
// - Throws DomainException with a code, never text (5.5)
// =========================================================
internal static class PlatformIdentityText
{
    public static string RequiredName(string? value)
    {
        string trimmed = value?.Trim() ?? string.Empty;

        if (trimmed.Length == 0)
        {
            throw new DomainException(PlatformIdentityErrors.NameRequired);
        }

        if (trimmed.Length > PlatformIdentityLimits.NameMaxLength)
        {
            throw new DomainException(PlatformIdentityErrors.NameTooLong);
        }

        return trimmed;
    }

    public static string? OptionalEmployeeNumber(string? value)
    {
        string? trimmed = Blank(value);

        if (trimmed is not null && trimmed.Length > PlatformIdentityLimits.EmployeeNumberMaxLength)
        {
            throw new DomainException(PlatformIdentityErrors.EmployeeNumberTooLong);
        }

        return trimmed;
    }

    public static string? OptionalDescription(string? value)
    {
        string? trimmed = Blank(value);

        if (trimmed is not null && trimmed.Length > PlatformIdentityLimits.DescriptionMaxLength)
        {
            throw new DomainException(PlatformIdentityErrors.DescriptionTooLong);
        }

        return trimmed;
    }

    public static string RequiredReason(string? value)
    {
        string trimmed = value?.Trim() ?? string.Empty;

        if (trimmed.Length == 0)
        {
            throw new DomainException(PlatformIdentityErrors.ReasonRequired);
        }

        if (trimmed.Length > PlatformIdentityLimits.ReasonMaxLength)
        {
            throw new DomainException(PlatformIdentityErrors.ReasonTooLong);
        }

        return trimmed;
    }

    public static Guid RequiredActor(Guid actorUserId)
    {
        if (actorUserId == Guid.Empty)
        {
            throw new DomainException(PlatformIdentityErrors.ActorRequired);
        }

        return actorUserId;
    }

    private static string? Blank(string? value)
    {
        string? trimmed = value?.Trim();

        return string.IsNullOrEmpty(trimmed) ? null : trimmed;
    }
}
