using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Receiving.GetOperationsParcelPhoto;

// =========================================================
// FILE: GetOperationsParcelPhotoResponse.cs
//
// PURPOSE:
// The file of one parcel photo, for staff (Slice 3B).
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class GetOperationsParcelPhotoResponse
{
    private GetOperationsParcelPhotoResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public byte[] Content { get; private init; } = Array.Empty<byte>();

    public string ContentType { get; private init; } = string.Empty;

    public static GetOperationsParcelPhotoResponse Success(byte[] content, string contentType)
    {
        return new GetOperationsParcelPhotoResponse { Succeeded = true, Content = content, ContentType = contentType };
    }

    public static GetOperationsParcelPhotoResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new GetOperationsParcelPhotoResponse { Error = error };
    }
}
