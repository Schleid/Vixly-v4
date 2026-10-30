using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Bundles.GetOperationsBundleLabel;

// =========================================================
// FILE: GetOperationsBundleLabelResponse.cs
//
// PURPOSE:
// The 4 x 6 label of a bundle (Slice 6A, D-2; owner, A-6).
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class GetOperationsBundleLabelResponse
{
    private GetOperationsBundleLabelResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public byte[] Pdf { get; private init; } = Array.Empty<byte>();

    public string Number { get; private init; } = string.Empty;

    public static GetOperationsBundleLabelResponse Success(byte[] pdf, string number)
    {
        return new GetOperationsBundleLabelResponse { Succeeded = true, Pdf = pdf, Number = number };
    }

    public static GetOperationsBundleLabelResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new GetOperationsBundleLabelResponse { Error = error };
    }
}
