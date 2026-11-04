using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Shipments.GetMyOperationsShipmentDocument;

// =========================================================
// FILE: GetMyOperationsShipmentDocumentResponse.cs
//
// PURPOSE:
// The packing list or the pro forma commercial invoice of a shipment, for the customer (Slice 5A).
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class GetMyOperationsShipmentDocumentResponse
{
    private GetMyOperationsShipmentDocumentResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public byte[] Content { get; private init; } = Array.Empty<byte>();

    public string FileName { get; private init; } = string.Empty;

    public static GetMyOperationsShipmentDocumentResponse Success(byte[] content, string fileName)
    {
        return new GetMyOperationsShipmentDocumentResponse { Succeeded = true, Content = content, FileName = fileName };
    }

    public static GetMyOperationsShipmentDocumentResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new GetMyOperationsShipmentDocumentResponse { Error = error };
    }
}
