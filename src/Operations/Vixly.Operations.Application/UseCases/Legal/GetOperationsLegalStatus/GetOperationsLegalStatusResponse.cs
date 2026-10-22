using Vixly.Operations.Application.Ports;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Legal.GetOperationsLegalStatus;

// =========================================================
// FILE: GetOperationsLegalStatusResponse.cs
//
// PURPOSE:
// Reads the tenant's Terms status and the version to accept (Slice 1C).
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class GetOperationsLegalStatusResponse
{
    private GetOperationsLegalStatusResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public OperationsLegalStatus? Status { get; private init; }

    public static GetOperationsLegalStatusResponse Success(OperationsLegalStatus? status)
    {
        return new GetOperationsLegalStatusResponse { Succeeded = true, Status = status };
    }

    public static GetOperationsLegalStatusResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new GetOperationsLegalStatusResponse { Error = error };
    }
}
