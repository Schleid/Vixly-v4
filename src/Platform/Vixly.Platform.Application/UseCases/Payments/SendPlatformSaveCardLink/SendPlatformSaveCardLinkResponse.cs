using Vixly.Shared.Kernel.Errors;

namespace Vixly.Platform.Application.UseCases.Payments.SendPlatformSaveCardLink;

// =========================================================
// FILE: SendPlatformSaveCardLinkResponse.cs
//
// PURPOSE:
// Emails the tenant's billing contact a link to save a card on the provider's page (D-P3).
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class SendPlatformSaveCardLinkResponse
{
    private SendPlatformSaveCardLinkResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public static SendPlatformSaveCardLinkResponse Success()
    {
        return new SendPlatformSaveCardLinkResponse { Succeeded = true };
    }

    public static SendPlatformSaveCardLinkResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new SendPlatformSaveCardLinkResponse { Error = error };
    }
}
