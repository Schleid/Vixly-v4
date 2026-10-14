using Vixly.Shared.Kernel.Errors;

namespace Vixly.Platform.Application.UseCases.Subscriptions.ApprovePlatformDiscount;

// =========================================================
// FILE: ApprovePlatformDiscountResponse.cs
//
// PURPOSE:
// Approves a discount waiting for the sales manager (D-S24).
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class ApprovePlatformDiscountResponse
{
    private ApprovePlatformDiscountResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public static ApprovePlatformDiscountResponse Success()
    {
        return new ApprovePlatformDiscountResponse { Succeeded = true };
    }

    public static ApprovePlatformDiscountResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new ApprovePlatformDiscountResponse { Error = error };
    }
}
