using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Dispatch.GetOperationsShipperLetter;

// =========================================================
// FILE: GetOperationsShipperLetterResponse.cs
//
// PURPOSE:
// The Shipper's Letter of Instruction of a dispatch as a PDF (Slice 6B step 2, D-6; as V3).
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class GetOperationsShipperLetterResponse
{
    private GetOperationsShipperLetterResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public byte[] Pdf { get; private init; } = Array.Empty<byte>();

    public string FileName { get; private init; } = string.Empty;

    public static GetOperationsShipperLetterResponse Success(byte[] pdf, string fileName)
    {
        return new GetOperationsShipperLetterResponse { Succeeded = true, Pdf = pdf, FileName = fileName };
    }

    public static GetOperationsShipperLetterResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new GetOperationsShipperLetterResponse { Error = error };
    }
}
