using Vixly.Operations.Domain.Supplies;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Supplies.GetOperationsSupplyLabels;

// =========================================================
// FILE: GetOperationsSupplyLabelsResponse.cs
//
// PURPOSE:
// A sheet of shelf labels for a supply: its name, its item code as a barcode and its price today (Slice 5E, S-3).
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class GetOperationsSupplyLabelsResponse
{
    private GetOperationsSupplyLabelsResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public byte[] Content { get; private init; } = Array.Empty<byte>();

    public string FileName { get; private init; } = string.Empty;

    public static GetOperationsSupplyLabelsResponse Success(byte[] content, string fileName)
    {
        return new GetOperationsSupplyLabelsResponse { Succeeded = true, Content = content, FileName = fileName };
    }

    public static GetOperationsSupplyLabelsResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new GetOperationsSupplyLabelsResponse { Error = error };
    }
}
