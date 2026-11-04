using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Billing.GetMyOperationsCreditStatement;

// =========================================================
// FILE: GetMyOperationsCreditStatementResponse.cs
//
// PURPOSE:
// The customer's own credit statement as a PDF on the portal (Slice 5F step 2b, K-8).
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class GetMyOperationsCreditStatementResponse
{
    private GetMyOperationsCreditStatementResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public byte[] Pdf { get; private init; } = Array.Empty<byte>();

    public static GetMyOperationsCreditStatementResponse Success(byte[] pdf)
    {
        return new GetMyOperationsCreditStatementResponse { Succeeded = true, Pdf = pdf };
    }

    public static GetMyOperationsCreditStatementResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new GetMyOperationsCreditStatementResponse { Error = error };
    }
}
