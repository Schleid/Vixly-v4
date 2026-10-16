using Vixly.Shared.Kernel.Errors;

namespace Vixly.Platform.Application.UseCases.Payments.RemovePlatformSavedCard;

// =========================================================
// FILE: RemovePlatformSavedCardResponse.cs
//
// PURPOSE:
// Removes a saved card and detaches it at the provider (D-P3).
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class RemovePlatformSavedCardResponse
{
    private RemovePlatformSavedCardResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public static RemovePlatformSavedCardResponse Success()
    {
        return new RemovePlatformSavedCardResponse { Succeeded = true };
    }

    public static RemovePlatformSavedCardResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new RemovePlatformSavedCardResponse { Error = error };
    }
}
