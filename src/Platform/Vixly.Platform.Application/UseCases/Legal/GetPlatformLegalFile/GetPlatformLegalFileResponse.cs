using Vixly.Platform.Application.Queries;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Platform.Application.UseCases.Legal.GetPlatformLegalFile;

// =========================================================
// FILE: GetPlatformLegalFileResponse.cs
//
// PURPOSE:
// Downloads one language file of a legal document for staff.
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class GetPlatformLegalFileResponse
{
    private GetPlatformLegalFileResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public PlatformLegalFileContent? File { get; private init; }

    public static GetPlatformLegalFileResponse Success(PlatformLegalFileContent? file)
    {
        return new GetPlatformLegalFileResponse { Succeeded = true, File = file };
    }

    public static GetPlatformLegalFileResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new GetPlatformLegalFileResponse { Error = error };
    }
}
