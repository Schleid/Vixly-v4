using Vixly.Shared.Kernel.Errors;

namespace Vixly.Platform.Application.UseCases.Legal.GetPlatformTenantLegalStatus;

// =========================================================
// FILE: GetPlatformTenantLegalStatusResponse.cs
//
// PURPOSE:
// Tells Operations where its tenant stands with the Terms and what to show the owner (D-Lg11).
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class GetPlatformTenantLegalStatusResponse
{
    private GetPlatformTenantLegalStatusResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public PlatformLegalStatusAnswer? Answer { get; private init; }

    public static GetPlatformTenantLegalStatusResponse Success(PlatformLegalStatusAnswer? answer)
    {
        return new GetPlatformTenantLegalStatusResponse { Succeeded = true, Answer = answer };
    }

    public static GetPlatformTenantLegalStatusResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new GetPlatformTenantLegalStatusResponse { Error = error };
    }
}
