using Vixly.Platform.Application.Queries;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Platform.Application.UseCases.Tenants.GetPlatformTenantChangeProof;

// =========================================================
// FILE: GetPlatformTenantChangeProofResponse.cs
//
// PURPOSE:
// A change request’s proof document (B12).
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class GetPlatformTenantChangeProofResponse
{
    private GetPlatformTenantChangeProofResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public PlatformTenantChangeProofFile? File { get; private init; }

    public static GetPlatformTenantChangeProofResponse Success(PlatformTenantChangeProofFile? file)
    {
        return new GetPlatformTenantChangeProofResponse { Succeeded = true, File = file };
    }

    public static GetPlatformTenantChangeProofResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);
        return new GetPlatformTenantChangeProofResponse { Error = error };
    }
}
