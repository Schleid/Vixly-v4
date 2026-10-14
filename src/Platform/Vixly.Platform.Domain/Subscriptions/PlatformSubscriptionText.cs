using Vixly.Shared.Kernel.Domain;

namespace Vixly.Platform.Domain.Subscriptions;

// =========================================================
// FILE: PlatformSubscriptionText.cs
//
// PURPOSE:
// Shared checks for reasons and actors in this area.
//
// RESPONSIBILITIES:
// - Trim and check reasons
// - Refuse an empty actor id
//
// IMPORTANT NOTES:
// - Internal helper; not part of the model surface
// =========================================================
internal static class PlatformSubscriptionText
{
    public static string Reason(string? value)
    {
        string trimmed = value?.Trim() ?? string.Empty;

        if (trimmed.Length == 0)
        {
            throw new DomainException(PlatformSubscriptionErrors.ReasonRequired);
        }

        if (trimmed.Length > PlatformSubscriptionLimits.ReasonMaxLength)
        {
            throw new DomainException(PlatformSubscriptionErrors.ReasonTooLong);
        }

        return trimmed;
    }

    public static Guid Actor(Guid actorUserId)
    {
        if (actorUserId == Guid.Empty)
        {
            throw new DomainException(PlatformSubscriptionErrors.ActorRequired);
        }

        return actorUserId;
    }

    public static string? OptionalText(string? value, int maxLength, Vixly.Shared.Kernel.Errors.Error tooLong)
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
}
