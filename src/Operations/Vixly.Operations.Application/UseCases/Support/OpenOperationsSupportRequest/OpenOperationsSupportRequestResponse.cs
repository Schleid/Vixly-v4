using Vixly.Operations.Application.Support;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Support.OpenOperationsSupportRequest;

// =========================================================
// FILE: OpenOperationsSupportRequestResponse.cs
//
// PURPOSE:
// A support agent opens a call ticket for staff: what the caller asks about their shipment or package (owner, 2026-10-29, S-3).
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class OpenOperationsSupportRequestResponse
{
    private OpenOperationsSupportRequestResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public Guid RequestId { get; private init; }

    public static OpenOperationsSupportRequestResponse Success(Guid requestId)
    {
        return new OpenOperationsSupportRequestResponse { Succeeded = true, RequestId = requestId };
    }

    public static OpenOperationsSupportRequestResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new OpenOperationsSupportRequestResponse { Error = error };
    }
}
