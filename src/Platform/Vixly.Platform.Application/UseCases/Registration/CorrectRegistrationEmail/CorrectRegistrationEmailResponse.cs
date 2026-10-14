using Vixly.Shared.Kernel.Errors;

namespace Vixly.Platform.Application.UseCases.Registration.CorrectRegistrationEmail;

// =========================================================
// FILE: CorrectRegistrationEmailResponse.cs
//
// PURPOSE:
// Result of CorrectRegistrationEmail.
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class CorrectRegistrationEmailResponse
{
    private CorrectRegistrationEmailResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public static CorrectRegistrationEmailResponse Success()
    {
        return new CorrectRegistrationEmailResponse { Succeeded = true };
    }

    public static CorrectRegistrationEmailResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new CorrectRegistrationEmailResponse { Error = error };
    }
}
