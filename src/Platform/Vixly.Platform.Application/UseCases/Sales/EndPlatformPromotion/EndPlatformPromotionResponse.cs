using Vixly.Shared.Kernel.Errors;

namespace Vixly.Platform.Application.UseCases.Sales.EndPlatformPromotion;

// =========================================================
// FILE: EndPlatformPromotionResponse.cs
//
// PURPOSE:
// Ends a promotion for good.
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class EndPlatformPromotionResponse
{
    private EndPlatformPromotionResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public static EndPlatformPromotionResponse Success()
    {
        return new EndPlatformPromotionResponse { Succeeded = true };
    }

    public static EndPlatformPromotionResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new EndPlatformPromotionResponse { Error = error };
    }
}
