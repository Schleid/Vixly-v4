using Vixly.Platform.Application.UseCases.Sales;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Platform.Application.UseCases.Sales.GetPlatformPromotion;

// =========================================================
// FILE: GetPlatformPromotionResponse.cs
//
// PURPOSE:
// Reads one promotion for its console page.
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class GetPlatformPromotionResponse
{
    private GetPlatformPromotionResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public PlatformPromotionDetail? Promotion { get; private init; }

    public PlatformSalesAbilities Abilities { get; private init; } = PlatformSalesAbilities.None;

    public static GetPlatformPromotionResponse Success(PlatformPromotionDetail? promotion, PlatformSalesAbilities abilities)
    {
        return new GetPlatformPromotionResponse { Succeeded = true, Promotion = promotion, Abilities = abilities };
    }

    public static GetPlatformPromotionResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new GetPlatformPromotionResponse { Error = error };
    }
}
