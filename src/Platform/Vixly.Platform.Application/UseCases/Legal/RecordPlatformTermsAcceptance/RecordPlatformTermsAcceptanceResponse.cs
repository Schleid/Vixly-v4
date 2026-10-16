using Vixly.Shared.Kernel.Errors;

namespace Vixly.Platform.Application.UseCases.Legal.RecordPlatformTermsAcceptance;

// =========================================================
// FILE: RecordPlatformTermsAcceptanceResponse.cs
//
// PURPOSE:
// Records a tenant's acceptance of the current Terms from a signed paper (D-Lg5).
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class RecordPlatformTermsAcceptanceResponse
{
    private RecordPlatformTermsAcceptanceResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public static RecordPlatformTermsAcceptanceResponse Success()
    {
        return new RecordPlatformTermsAcceptanceResponse { Succeeded = true };
    }

    public static RecordPlatformTermsAcceptanceResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new RecordPlatformTermsAcceptanceResponse { Error = error };
    }
}
