using Vixly.Shared.Kernel.Errors;

namespace Vixly.Platform.Application.UseCases.Legal.SendPlatformTermsReminder;

// =========================================================
// FILE: SendPlatformTermsReminderResponse.cs
//
// PURPOSE:
// Sends a Terms reminder to a tenant's owner now, with a reason (D-Lg7).
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class SendPlatformTermsReminderResponse
{
    private SendPlatformTermsReminderResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public static SendPlatformTermsReminderResponse Success()
    {
        return new SendPlatformTermsReminderResponse { Succeeded = true };
    }

    public static SendPlatformTermsReminderResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new SendPlatformTermsReminderResponse { Error = error };
    }
}
