using Vixly.Shared.Kernel.Errors;

namespace Vixly.Platform.Application.UseCases.Legal.SetPlatformLegalFile;

// =========================================================
// FILE: SetPlatformLegalFileResponse.cs
//
// PURPOSE:
// Uploads or replaces the PDF of a draft in one language, with its summary.
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class SetPlatformLegalFileResponse
{
    private SetPlatformLegalFileResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public static SetPlatformLegalFileResponse Success()
    {
        return new SetPlatformLegalFileResponse { Succeeded = true };
    }

    public static SetPlatformLegalFileResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new SetPlatformLegalFileResponse { Error = error };
    }
}
