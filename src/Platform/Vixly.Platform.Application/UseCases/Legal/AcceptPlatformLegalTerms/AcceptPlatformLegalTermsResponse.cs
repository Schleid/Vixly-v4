using Vixly.Shared.Kernel.Errors;

namespace Vixly.Platform.Application.UseCases.Legal.AcceptPlatformLegalTerms;

// =========================================================
// FILE: AcceptPlatformLegalTermsResponse.cs
//
// PURPOSE:
// Records the owner's acceptance of the current Terms, sent by Operations (D-Lg5, D-Lg11).
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class AcceptPlatformLegalTermsResponse
{
    private AcceptPlatformLegalTermsResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public static AcceptPlatformLegalTermsResponse Success()
    {
        return new AcceptPlatformLegalTermsResponse { Succeeded = true };
    }

    public static AcceptPlatformLegalTermsResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new AcceptPlatformLegalTermsResponse { Error = error };
    }
}
