using Vixly.Platform.Application.UseCases.Sales;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Platform.Application.UseCases.Registration.CheckRegistrationPromotion;

// =========================================================
// FILE: CheckRegistrationPromotionResponse.cs
//
// PURPOSE:
// Checks a promotion code at the plan step of registration (D-S20).
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class CheckRegistrationPromotionResponse
{
    private CheckRegistrationPromotionResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public string Name { get; private init; } = string.Empty;

    public PlatformDiscountTermsItem? Terms { get; private init; }

    public static CheckRegistrationPromotionResponse Success(string name, PlatformDiscountTermsItem? terms)
    {
        return new CheckRegistrationPromotionResponse { Succeeded = true, Name = name, Terms = terms };
    }

    public static CheckRegistrationPromotionResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new CheckRegistrationPromotionResponse { Error = error };
    }
}
