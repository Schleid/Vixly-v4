using Vixly.Shared.Kernel.Errors;

namespace Vixly.Platform.Application.UseCases.Payments.GetPlatformZelleClaim;

// =========================================================
// FILE: GetPlatformZelleClaimResponse.cs
//
// PURPOSE:
// Reads one Zelle report with the invoice balance and what the reader may do.
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class GetPlatformZelleClaimResponse
{
    private GetPlatformZelleClaimResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public PlatformZelleClaimDetail? Detail { get; private init; }

    public static GetPlatformZelleClaimResponse Success(PlatformZelleClaimDetail? detail)
    {
        return new GetPlatformZelleClaimResponse { Succeeded = true, Detail = detail };
    }

    public static GetPlatformZelleClaimResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new GetPlatformZelleClaimResponse { Error = error };
    }
}
