using Vixly.Shared.Kernel.Errors;

namespace Vixly.Platform.Application.UseCases.Sales.SubmitPlatformPromotion;

// =========================================================
// FILE: SubmitPlatformPromotionResponse.cs
//
// PURPOSE:
// Submits a promotion for CFO approval (D-S26).
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class SubmitPlatformPromotionResponse
{
    private SubmitPlatformPromotionResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public static SubmitPlatformPromotionResponse Success()
    {
        return new SubmitPlatformPromotionResponse { Succeeded = true };
    }

    public static SubmitPlatformPromotionResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new SubmitPlatformPromotionResponse { Error = error };
    }
}
