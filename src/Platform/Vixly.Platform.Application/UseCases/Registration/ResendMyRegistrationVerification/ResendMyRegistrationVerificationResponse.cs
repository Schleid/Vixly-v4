using Vixly.Shared.Kernel.Errors;

namespace Vixly.Platform.Application.UseCases.Registration.ResendMyRegistrationVerification;

// =========================================================
// FILE: ResendMyRegistrationVerificationResponse.cs
//
// PURPOSE:
// Result of ResendMyRegistrationVerification.
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class ResendMyRegistrationVerificationResponse
{
    private ResendMyRegistrationVerificationResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public static ResendMyRegistrationVerificationResponse Success()
    {
        return new ResendMyRegistrationVerificationResponse { Succeeded = true };
    }

    public static ResendMyRegistrationVerificationResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new ResendMyRegistrationVerificationResponse { Error = error };
    }
}
