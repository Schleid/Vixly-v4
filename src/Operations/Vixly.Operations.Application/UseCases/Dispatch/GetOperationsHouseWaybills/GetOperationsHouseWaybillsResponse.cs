using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Dispatch.GetOperationsHouseWaybills;

// =========================================================
// FILE: GetOperationsHouseWaybillsResponse.cs
//
// PURPOSE:
// The house air waybills of a closed dispatch as one PDF, or of one of its shipments (Slice 6B step 2, D-6; owner, A-5).
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class GetOperationsHouseWaybillsResponse
{
    private GetOperationsHouseWaybillsResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public byte[] Pdf { get; private init; } = Array.Empty<byte>();

    public string FileName { get; private init; } = string.Empty;

    public static GetOperationsHouseWaybillsResponse Success(byte[] pdf, string fileName)
    {
        return new GetOperationsHouseWaybillsResponse { Succeeded = true, Pdf = pdf, FileName = fileName };
    }

    public static GetOperationsHouseWaybillsResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new GetOperationsHouseWaybillsResponse { Error = error };
    }
}
