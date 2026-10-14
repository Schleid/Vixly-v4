using Vixly.Shared.Kernel.Errors;

namespace Vixly.Platform.Application.UseCases.Sales.RejectPlatformPromotion;

// =========================================================
// FILE: RejectPlatformPromotionResponse.cs
//
// PURPOSE:
// Rejects a submitted promotion with a reason (D-S26).
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class RejectPlatformPromotionResponse
{
    private RejectPlatformPromotionResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public static RejectPlatformPromotionResponse Success()
    {
        return new RejectPlatformPromotionResponse { Succeeded = true };
    }

    public static RejectPlatformPromotionResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new RejectPlatformPromotionResponse { Error = error };
    }
}
