using Vixly.Shared.Kernel.Errors;

namespace Vixly.Platform.Application.UseCases.Sales.CreatePlatformPromotion;

// =========================================================
// FILE: CreatePlatformPromotionResponse.cs
//
// PURPOSE:
// Designs a new promotion as a draft (D-S20, D-S26).
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class CreatePlatformPromotionResponse
{
    private CreatePlatformPromotionResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public Guid PromotionId { get; private init; }

    public static CreatePlatformPromotionResponse Success(Guid promotionId)
    {
        return new CreatePlatformPromotionResponse { Succeeded = true, PromotionId = promotionId };
    }

    public static CreatePlatformPromotionResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new CreatePlatformPromotionResponse { Error = error };
    }
}
