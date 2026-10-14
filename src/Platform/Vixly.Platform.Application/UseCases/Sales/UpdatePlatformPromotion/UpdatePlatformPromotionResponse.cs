using Vixly.Shared.Kernel.Errors;

namespace Vixly.Platform.Application.UseCases.Sales.UpdatePlatformPromotion;

// =========================================================
// FILE: UpdatePlatformPromotionResponse.cs
//
// PURPOSE:
// Changes a draft or rejected promotion; it becomes a draft again.
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class UpdatePlatformPromotionResponse
{
    private UpdatePlatformPromotionResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public static UpdatePlatformPromotionResponse Success()
    {
        return new UpdatePlatformPromotionResponse { Succeeded = true };
    }

    public static UpdatePlatformPromotionResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new UpdatePlatformPromotionResponse { Error = error };
    }
}
