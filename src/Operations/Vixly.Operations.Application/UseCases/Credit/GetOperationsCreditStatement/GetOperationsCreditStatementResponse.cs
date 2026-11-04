using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Credit.GetOperationsCreditStatement;

// =========================================================
// FILE: GetOperationsCreditStatementResponse.cs
//
// PURPOSE:
// A credit account's statement as a PDF (Slice 5F step 2b, K-7).
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class GetOperationsCreditStatementResponse
{
    private GetOperationsCreditStatementResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public byte[] Pdf { get; private init; } = Array.Empty<byte>();

    public static GetOperationsCreditStatementResponse Success(byte[] pdf)
    {
        return new GetOperationsCreditStatementResponse { Succeeded = true, Pdf = pdf };
    }

    public static GetOperationsCreditStatementResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new GetOperationsCreditStatementResponse { Error = error };
    }
}
