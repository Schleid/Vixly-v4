using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Dispatch.GetOperationsDispatchManifest;

// =========================================================
// FILE: GetOperationsDispatchManifestResponse.cs
//
// PURPOSE:
// The dispatch manifest as a PDF (Slice 6B, D-6): legs, bundles, shipments and consignees, for the carrier and customs.
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class GetOperationsDispatchManifestResponse
{
    private GetOperationsDispatchManifestResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public byte[] Pdf { get; private init; } = Array.Empty<byte>();

    public string Number { get; private init; } = string.Empty;

    public static GetOperationsDispatchManifestResponse Success(byte[] pdf, string number)
    {
        return new GetOperationsDispatchManifestResponse { Succeeded = true, Pdf = pdf, Number = number };
    }

    public static GetOperationsDispatchManifestResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new GetOperationsDispatchManifestResponse { Error = error };
    }
}
