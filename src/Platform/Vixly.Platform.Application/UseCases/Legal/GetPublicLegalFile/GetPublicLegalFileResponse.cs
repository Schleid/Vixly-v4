using Vixly.Platform.Application.Queries;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Platform.Application.UseCases.Legal.GetPublicLegalFile;

// =========================================================
// FILE: GetPublicLegalFileResponse.cs
//
// PURPOSE:
// Downloads a published legal document in one language (public pages, owners).
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class GetPublicLegalFileResponse
{
    private GetPublicLegalFileResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public PlatformLegalFileContent? File { get; private init; }

    public static GetPublicLegalFileResponse Success(PlatformLegalFileContent? file)
    {
        return new GetPublicLegalFileResponse { Succeeded = true, File = file };
    }

    public static GetPublicLegalFileResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new GetPublicLegalFileResponse { Error = error };
    }
}
