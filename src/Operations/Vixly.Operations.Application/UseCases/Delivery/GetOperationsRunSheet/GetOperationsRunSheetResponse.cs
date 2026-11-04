using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Delivery.GetOperationsRunSheet;

// =========================================================
// FILE: GetOperationsRunSheetResponse.cs
//
// PURPOSE:
// A delivery route's run sheet as a PDF (Slice 7 step 3, R-7, R-12).
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class GetOperationsRunSheetResponse
{
    private GetOperationsRunSheetResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public byte[] Pdf { get; private init; } = Array.Empty<byte>();

    public string Number { get; private init; } = string.Empty;

    public static GetOperationsRunSheetResponse Success(byte[] pdf, string number)
    {
        return new GetOperationsRunSheetResponse { Succeeded = true, Pdf = pdf, Number = number };
    }

    public static GetOperationsRunSheetResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new GetOperationsRunSheetResponse { Error = error };
    }
}
