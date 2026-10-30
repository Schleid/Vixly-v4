using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Legal.AcceptOperationsLegalTerms;

// =========================================================
// FILE: AcceptOperationsLegalTermsResponse.cs
//
// PURPOSE:
// Records the Owner's acceptance of the Terms version shown (Slice 1C).
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class AcceptOperationsLegalTermsResponse
{
    private AcceptOperationsLegalTermsResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public static AcceptOperationsLegalTermsResponse Success()
    {
        return new AcceptOperationsLegalTermsResponse { Succeeded = true };
    }

    public static AcceptOperationsLegalTermsResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new AcceptOperationsLegalTermsResponse { Error = error };
    }
}
