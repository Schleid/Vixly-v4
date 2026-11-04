using Vixly.Shared.Kernel.Errors;

namespace Vixly.Platform.Application.UseCases.Licensing.CompletePlatformSecondFactorReset;

// =========================================================
// FILE: CompletePlatformSecondFactorResetResponse.cs
//
// PURPOSE:
// Uses an emailed second-factor reset link on the tenant host (D-O13).
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class CompletePlatformSecondFactorResetResponse
{
    private CompletePlatformSecondFactorResetResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public Guid PlatformTenantId { get; private init; }

    public static CompletePlatformSecondFactorResetResponse Success(Guid platformTenantId)
    {
        return new CompletePlatformSecondFactorResetResponse { Succeeded = true, PlatformTenantId = platformTenantId };
    }

    public static CompletePlatformSecondFactorResetResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new CompletePlatformSecondFactorResetResponse { Error = error };
    }
}
