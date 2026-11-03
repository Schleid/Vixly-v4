using Vixly.Operations.Application.Support;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Support.GetOperationsSupportCase;

// =========================================================
// FILE: GetOperationsSupportCaseResponse.cs
//
// PURPOSE:
// Step 2 of a support call: one shipment or package by its tracking number, only when it is the caller's (owner, 2026-10-29, design 9.1, 9.2).
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class GetOperationsSupportCaseResponse
{
    private GetOperationsSupportCaseResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public OperationsSupportCase? Case { get; private init; }

    public static GetOperationsSupportCaseResponse Success(OperationsSupportCase found)
    {
        return new GetOperationsSupportCaseResponse { Succeeded = true, Case = found };
    }

    public static GetOperationsSupportCaseResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new GetOperationsSupportCaseResponse { Error = error };
    }
}
