using Vixly.Operations.Application.Support;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Support.FindOperationsSupportCaller;

// =========================================================
// FILE: FindOperationsSupportCallerResponse.cs
//
// PURPOSE:
// Step 1 of a support call: the caller's full phone number names one account, briefly (owner, 2026-10-29, S-7).
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class FindOperationsSupportCallerResponse
{
    private FindOperationsSupportCallerResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public OperationsSupportCaller? Caller { get; private init; }

    public static FindOperationsSupportCallerResponse Success(OperationsSupportCaller? caller)
    {
        return new FindOperationsSupportCallerResponse { Succeeded = true, Caller = caller };
    }

    public static FindOperationsSupportCallerResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new FindOperationsSupportCallerResponse { Error = error };
    }
}
