using Vixly.Shared.Kernel.Errors;

namespace Vixly.Platform.Application.UseCases.Licensing.RequestPlatformSecondFactorReset;

// =========================================================
// FILE: RequestPlatformSecondFactorResetResponse.cs
//
// PURPOSE:
// Vixly staff ask to reset the Owner's second factor; a second staff member must approve (D-O13).
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class RequestPlatformSecondFactorResetResponse
{
    private RequestPlatformSecondFactorResetResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public Guid RequestId { get; private init; }

    public static RequestPlatformSecondFactorResetResponse Success(Guid requestId)
    {
        return new RequestPlatformSecondFactorResetResponse { Succeeded = true, RequestId = requestId };
    }

    public static RequestPlatformSecondFactorResetResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new RequestPlatformSecondFactorResetResponse { Error = error };
    }
}
