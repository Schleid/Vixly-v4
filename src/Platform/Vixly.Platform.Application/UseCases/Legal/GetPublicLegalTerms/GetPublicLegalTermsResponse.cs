using Vixly.Shared.Kernel.Errors;

namespace Vixly.Platform.Application.UseCases.Legal.GetPublicLegalTerms;

// =========================================================
// FILE: GetPublicLegalTermsResponse.cs
//
// PURPOSE:
// Reads the current Terms in a language for the registration page (D-Lg9).
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class GetPublicLegalTermsResponse
{
    private GetPublicLegalTermsResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public PlatformLegalTerms? Terms { get; private init; }

    public static GetPublicLegalTermsResponse Success(PlatformLegalTerms? terms)
    {
        return new GetPublicLegalTermsResponse { Succeeded = true, Terms = terms };
    }

    public static GetPublicLegalTermsResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new GetPublicLegalTermsResponse { Error = error };
    }
}
