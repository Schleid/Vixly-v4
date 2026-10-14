using Vixly.Shared.Kernel.Errors;

namespace Vixly.Platform.Application.UseCases.MyAccount.CompletePasskeyRegistration;

// =========================================================
// FILE: CompletePasskeyRegistrationResponse.cs
//
// PURPOSE:
// Result of CompletePasskeyRegistration.
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class CompletePasskeyRegistrationResponse
{
    private CompletePasskeyRegistrationResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public Guid PasskeyId { get; private init; }

    public static CompletePasskeyRegistrationResponse Success(Guid passkeyId)
    {
        return new CompletePasskeyRegistrationResponse { Succeeded = true, PasskeyId = passkeyId };
    }

    public static CompletePasskeyRegistrationResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new CompletePasskeyRegistrationResponse { Error = error };
    }
}
