using Vixly.Shared.Kernel.Errors;

namespace Vixly.Platform.Application.UseCases.Sales.SetPlatformPromotionName;

// =========================================================
// FILE: SetPlatformPromotionNameResponse.cs
//
// PURPOSE:
// Sets a promotion's name in one language (ADR-0005).
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class SetPlatformPromotionNameResponse
{
    private SetPlatformPromotionNameResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public static SetPlatformPromotionNameResponse Success()
    {
        return new SetPlatformPromotionNameResponse { Succeeded = true };
    }

    public static SetPlatformPromotionNameResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new SetPlatformPromotionNameResponse { Error = error };
    }
}
