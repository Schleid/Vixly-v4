using Vixly.Shared.Kernel.Errors;

namespace Vixly.Platform.Application.UseCases.Registrations.ResendRegistrationVerification;

// =========================================================
// FILE: ResendRegistrationVerificationResponse.cs
//
// PURPOSE:
// Result of ResendRegistrationVerification.
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class ResendRegistrationVerificationResponse
{
    private ResendRegistrationVerificationResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public static ResendRegistrationVerificationResponse Success()
    {
        return new ResendRegistrationVerificationResponse { Succeeded = true };
    }

    public static ResendRegistrationVerificationResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new ResendRegistrationVerificationResponse { Error = error };
    }
}
