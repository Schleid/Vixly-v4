using Vixly.Shared.Kernel.Errors;

namespace Vixly.Platform.Application.UseCases.Subscriptions.EndPlatformDiscount;

// =========================================================
// FILE: EndPlatformDiscountResponse.cs
//
// PURPOSE:
// Ends the tenant's current discount now, with a reason.
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class EndPlatformDiscountResponse
{
    private EndPlatformDiscountResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public static EndPlatformDiscountResponse Success()
    {
        return new EndPlatformDiscountResponse { Succeeded = true };
    }

    public static EndPlatformDiscountResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new EndPlatformDiscountResponse { Error = error };
    }
}
