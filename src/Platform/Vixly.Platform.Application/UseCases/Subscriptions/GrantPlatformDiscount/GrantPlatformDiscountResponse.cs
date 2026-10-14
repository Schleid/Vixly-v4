using Vixly.Shared.Kernel.Errors;

namespace Vixly.Platform.Application.UseCases.Subscriptions.GrantPlatformDiscount;

// =========================================================
// FILE: GrantPlatformDiscountResponse.cs
//
// PURPOSE:
// Grants a discount to one tenant, or asks the sales manager when it is beyond the limits (D-S18, D-S22, D-S24).
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class GrantPlatformDiscountResponse
{
    private GrantPlatformDiscountResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public bool NeedsApproval { get; private init; }

    public static GrantPlatformDiscountResponse Success(bool needsApproval)
    {
        return new GrantPlatformDiscountResponse { Succeeded = true, NeedsApproval = needsApproval };
    }

    public static GrantPlatformDiscountResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new GrantPlatformDiscountResponse { Error = error };
    }
}
