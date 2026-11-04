using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Dispatch.GetOperationsBundleManifest;

// =========================================================
// FILE: GetOperationsBundleManifestResponse.cs
//
// PURPOSE:
// A bundle's manifest as a PDF (Slice 6B, D-6): the shipments and parcels inside it.
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class GetOperationsBundleManifestResponse
{
    private GetOperationsBundleManifestResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public byte[] Pdf { get; private init; } = Array.Empty<byte>();

    public string Number { get; private init; } = string.Empty;

    public static GetOperationsBundleManifestResponse Success(byte[] pdf, string number)
    {
        return new GetOperationsBundleManifestResponse { Succeeded = true, Pdf = pdf, Number = number };
    }

    public static GetOperationsBundleManifestResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new GetOperationsBundleManifestResponse { Error = error };
    }
}
