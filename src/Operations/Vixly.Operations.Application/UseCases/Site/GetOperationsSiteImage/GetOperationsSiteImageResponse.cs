using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Site.GetOperationsSiteImage;

// =========================================================
// FILE: GetOperationsSiteImageResponse.cs
//
// PURPOSE:
// One image of the tenant site (Slice 1B).
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class GetOperationsSiteImageResponse
{
    private GetOperationsSiteImageResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public string ContentType { get; private init; } = string.Empty;

    public byte[] Content { get; private init; } = Array.Empty<byte>();

    public string Sha256 { get; private init; } = string.Empty;

    public bool IsPublished { get; private init; }

    public static GetOperationsSiteImageResponse Success(string contentType, byte[] content, string sha256, bool isPublished)
    {
        return new GetOperationsSiteImageResponse { Succeeded = true, ContentType = contentType, Content = content, Sha256 = sha256, IsPublished = isPublished };
    }

    public static GetOperationsSiteImageResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new GetOperationsSiteImageResponse { Error = error };
    }
}
