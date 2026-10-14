using Vixly.Shared.Kernel.Errors;

namespace Vixly.Platform.Application.UseCases.Subscriptions.RejectPlatformDiscount;

// =========================================================
// FILE: RejectPlatformDiscountResponse.cs
//
// PURPOSE:
// Rejects a discount waiting for the sales manager, with a reason (D-S24).
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class RejectPlatformDiscountResponse
{
    private RejectPlatformDiscountResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public static RejectPlatformDiscountResponse Success()
    {
        return new RejectPlatformDiscountResponse { Succeeded = true };
    }

    public static RejectPlatformDiscountResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new RejectPlatformDiscountResponse { Error = error };
    }
}
