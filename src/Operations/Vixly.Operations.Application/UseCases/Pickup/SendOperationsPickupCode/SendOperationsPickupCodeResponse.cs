using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Pickup.SendOperationsPickupCode;

// =========================================================
// FILE: SendOperationsPickupCodeResponse.cs
//
// PURPOSE:
// Send the customer a new pickup code (Slice 7 step 2, R-5).
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class SendOperationsPickupCodeResponse
{
    private SendOperationsPickupCodeResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public static SendOperationsPickupCodeResponse Success()
    {
        return new SendOperationsPickupCodeResponse { Succeeded = true };
    }

    public static SendOperationsPickupCodeResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new SendOperationsPickupCodeResponse { Error = error };
    }
}
