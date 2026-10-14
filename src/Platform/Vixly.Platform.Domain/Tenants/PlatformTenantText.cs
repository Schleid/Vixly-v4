using Vixly.Shared.Kernel.Domain;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Platform.Domain.Tenants;

// =========================================================
// FILE: PlatformTenantText.cs
//
// PURPOSE:
// Shared validation for the free-text fields of the tenant model.
//
// RESPONSIBILITIES:
// - Trim and validate required and optional text
// - Turn blanks into null
//
// IMPORTANT NOTES:
// - Internal: only tenant entities use it
// - Throws DomainException with a code, never text (5.5)
// =========================================================
internal static class PlatformTenantText
{
    public static string Required(string? value, int maxLength, Error required, Error tooLong)
    {
        string trimmed = value?.Trim() ?? string.Empty;

        if (trimmed.Length == 0)
        {
            throw new DomainException(required);
        }

        if (trimmed.Length > maxLength)
        {
            throw new DomainException(tooLong);
        }

        return trimmed;
    }

    public static string? Optional(string? value, int maxLength, Error tooLong)
    {
        string trimmed = value?.Trim() ?? string.Empty;

        if (trimmed.Length == 0)
        {
            return null;
        }

        if (trimmed.Length > maxLength)
        {
            throw new DomainException(tooLong);
        }

        return trimmed;
    }

    public static string Reason(string? value)
    {
        return Required(
            value,
            PlatformTenantLimits.ReasonMaxLength,
            PlatformTenantErrors.ReasonRequired,
            PlatformTenantErrors.ReasonTooLong);
    }

    public static Guid Actor(Guid actorUserId)
    {
        if (actorUserId == Guid.Empty)
        {
            throw new DomainException(PlatformTenantErrors.ActorRequired);
        }

        return actorUserId;
    }
}
