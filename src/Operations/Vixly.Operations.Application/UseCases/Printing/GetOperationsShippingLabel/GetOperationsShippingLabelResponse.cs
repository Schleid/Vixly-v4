using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Printing.GetOperationsShippingLabel;

// =========================================================
// FILE: GetOperationsShippingLabelResponse.cs
//
// PURPOSE:
// The shipping label of a cleared parcel as a PDF, for a station that prints from the browser (Slice 4D step 2).
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class GetOperationsShippingLabelResponse
{
    private GetOperationsShippingLabelResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public byte[] Content { get; private init; } = Array.Empty<byte>();

    public string FileName { get; private init; } = string.Empty;

    public static GetOperationsShippingLabelResponse Success(byte[] content, string fileName)
    {
        return new GetOperationsShippingLabelResponse { Succeeded = true, Content = content, FileName = fileName };
    }

    public static GetOperationsShippingLabelResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new GetOperationsShippingLabelResponse { Error = error };
    }
}
