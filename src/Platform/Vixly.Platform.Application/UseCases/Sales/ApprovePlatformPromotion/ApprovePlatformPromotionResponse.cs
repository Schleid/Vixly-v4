using Vixly.Shared.Kernel.Errors;

namespace Vixly.Platform.Application.UseCases.Sales.ApprovePlatformPromotion;

// =========================================================
// FILE: ApprovePlatformPromotionResponse.cs
//
// PURPOSE:
// Approves a submitted promotion; it goes live (D-S26).
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class ApprovePlatformPromotionResponse
{
    private ApprovePlatformPromotionResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public static ApprovePlatformPromotionResponse Success()
    {
        return new ApprovePlatformPromotionResponse { Succeeded = true };
    }

    public static ApprovePlatformPromotionResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new ApprovePlatformPromotionResponse { Error = error };
    }
}
