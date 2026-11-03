using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Billing.GetMyOperationsZelleQr;

// =========================================================
// FILE: GetMyOperationsZelleQrResponse.cs
//
// PURPOSE:
// The company's Zelle QR picture for a signed-in customer paying on the portal (owner, 2026-10-27).
//
// RESPONSIBILITIES:
// - Report the picture and its type, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class GetMyOperationsZelleQrResponse
{
    private GetMyOperationsZelleQrResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public byte[] Content { get; private init; } = Array.Empty<byte>();

    public string ContentType { get; private init; } = string.Empty;

    public static GetMyOperationsZelleQrResponse Success(byte[] content, string contentType) => new() { Succeeded = true, Content = content, ContentType = contentType };

    public static GetMyOperationsZelleQrResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new GetMyOperationsZelleQrResponse { Error = error };
    }
}
