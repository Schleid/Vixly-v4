using Vixly.Shared.Kernel.Errors;

namespace Vixly.Platform.Application.UseCases.Sales.PausePlatformPromotion;

// =========================================================
// FILE: PausePlatformPromotionResponse.cs
//
// PURPOSE:
// Pauses an active promotion.
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class PausePlatformPromotionResponse
{
    private PausePlatformPromotionResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public static PausePlatformPromotionResponse Success()
    {
        return new PausePlatformPromotionResponse { Succeeded = true };
    }

    public static PausePlatformPromotionResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new PausePlatformPromotionResponse { Error = error };
    }
}
