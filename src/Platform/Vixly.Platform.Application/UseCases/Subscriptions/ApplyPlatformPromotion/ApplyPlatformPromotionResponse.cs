using Vixly.Shared.Kernel.Errors;

namespace Vixly.Platform.Application.UseCases.Subscriptions.ApplyPlatformPromotion;

// =========================================================
// FILE: ApplyPlatformPromotionResponse.cs
//
// PURPOSE:
// Applies an approved promotion code to a tenant's subscription (D-S20).
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class ApplyPlatformPromotionResponse
{
    private ApplyPlatformPromotionResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public static ApplyPlatformPromotionResponse Success()
    {
        return new ApplyPlatformPromotionResponse { Succeeded = true };
    }

    public static ApplyPlatformPromotionResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new ApplyPlatformPromotionResponse { Error = error };
    }
}
