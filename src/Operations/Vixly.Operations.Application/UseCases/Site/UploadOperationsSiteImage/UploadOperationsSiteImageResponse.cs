using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Site.UploadOperationsSiteImage;

// =========================================================
// FILE: UploadOperationsSiteImageResponse.cs
//
// PURPOSE:
// Adds a logo, favicon or hero image to the draft site (Slice 1B).
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class UploadOperationsSiteImageResponse
{
    private UploadOperationsSiteImageResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public Guid ImageId { get; private init; }

    public static UploadOperationsSiteImageResponse Success(Guid imageId)
    {
        return new UploadOperationsSiteImageResponse { Succeeded = true, ImageId = imageId };
    }

    public static UploadOperationsSiteImageResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new UploadOperationsSiteImageResponse { Error = error };
    }
}
