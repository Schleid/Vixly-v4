using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Receiving.GetMyOperationsParcelPhoto;

// =========================================================
// FILE: GetMyOperationsParcelPhotoResponse.cs
//
// PURPOSE:
// The file of a photo of a customer's own package (Slice 3B).
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class GetMyOperationsParcelPhotoResponse
{
    private GetMyOperationsParcelPhotoResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public byte[] Content { get; private init; } = Array.Empty<byte>();

    public string ContentType { get; private init; } = string.Empty;

    public static GetMyOperationsParcelPhotoResponse Success(byte[] content, string contentType)
    {
        return new GetMyOperationsParcelPhotoResponse { Succeeded = true, Content = content, ContentType = contentType };
    }

    public static GetMyOperationsParcelPhotoResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new GetMyOperationsParcelPhotoResponse { Error = error };
    }
}
