using Vixly.Shared.Kernel.Errors;

namespace Vixly.Platform.Application.UseCases.Sales.ResumePlatformPromotion;

// =========================================================
// FILE: ResumePlatformPromotionResponse.cs
//
// PURPOSE:
// Resumes a paused promotion.
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class ResumePlatformPromotionResponse
{
    private ResumePlatformPromotionResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public static ResumePlatformPromotionResponse Success()
    {
        return new ResumePlatformPromotionResponse { Succeeded = true };
    }

    public static ResumePlatformPromotionResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new ResumePlatformPromotionResponse { Error = error };
    }
}
