using Vixly.Shared.Kernel.Errors;

namespace Vixly.Platform.Application.UseCases.Payments.TurnOffPlatformAutoCharge;

// =========================================================
// FILE: TurnOffPlatformAutoChargeResponse.cs
//
// PURPOSE:
// Stops automatic charging for a tenant; only the tenant can turn it back on (D-P4).
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class TurnOffPlatformAutoChargeResponse
{
    private TurnOffPlatformAutoChargeResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public static TurnOffPlatformAutoChargeResponse Success()
    {
        return new TurnOffPlatformAutoChargeResponse { Succeeded = true };
    }

    public static TurnOffPlatformAutoChargeResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new TurnOffPlatformAutoChargeResponse { Error = error };
    }
}
